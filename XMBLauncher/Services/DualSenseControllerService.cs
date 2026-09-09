using System;
using System.Diagnostics;
using System.Threading;
using HidSharp;

namespace XMBLauncher.Services;

public enum DualSenseAction
{
    Up,
    Down,
    Left,
    Right,
    Launch,
    Back,
    Import,
    Remove
}

internal enum DualSenseDirection
{
    None,
    Up,
    Down,
    Left,
    Right
}

public sealed class DualSenseControllerService : IDisposable
{
    private const int VendorId = 0x054C;
    private const int ProductId = 0x0CE6;

    private const byte UsbReportId = 0x01;
    private const byte BluetoothReportId = 0x31;
    private const double EnterDeadzone = 0.62;
    private const double ExitDeadzone = 0.42;

    private const int InitialRepeatDelayMs = 350;
    private const int RepeatIntervalMs = 150;

    private HidDevice? device;
    private HidStream? stream;

    private Thread? workerThread;

    private volatile bool running;

    private readonly object streamLock = new();

    private DualSenseDirection currentDirection =
        DualSenseDirection.None;

    private DualSenseDirection lastDirection =
        DualSenseDirection.None;

    private DateTime directionStartedAt =
        DateTime.MinValue;

    private DateTime lastRepeatAt =
        DateTime.MinValue;

    private bool crossPressed;
    private bool circlePressed;
    private bool squarePressed;
    private bool trianglePressed;
    private bool optionsPressed;

    public event EventHandler<DualSenseAction>? ActionTriggered;

    public bool IsConnected
    {
        get
        {
            return device != null &&
                   stream != null &&
                   running;
        }
    }

    public void Start()
    {
        if (running)
            return;

        running = true;

        workerThread =
            new Thread(InputLoop)
            {
                IsBackground = true,
                Name = "DualSense Input"
            };

        workerThread.Start();
    }

    public void Stop()
    {
        running = false;

        CloseController();

        Thread? thread = workerThread;

        if (thread != null &&
            thread != Thread.CurrentThread)
        {
            try
            {
                thread.Join(500);
            }
            catch
            {
            }
        }

        workerThread = null;
    }

    private void InputLoop()
    {
        while (running)
        {
            try
            {
                if (!EnsureControllerConnected())
                {
                    Thread.Sleep(1000);
                    continue;
                }

                HidStream? currentStream;

                lock (streamLock)
                {
                    currentStream = stream;
                }

                if (currentStream == null)
                {
                    Thread.Sleep(250);
                    continue;
                }

                byte[] report =
                    new byte[128];

                int bytesRead =
                    currentStream.Read(
                        report,
                        0,
                        report.Length);

                if (bytesRead <= 0)
                {
                    ResetInputState();
                    CloseController();
                    continue;
                }

                ProcessReport(
                    report,
                    bytesRead);
            }
            catch
            {
                ResetInputState();
                CloseController();

                if (running)
                    Thread.Sleep(750);
            }
        }
    }

    private bool EnsureControllerConnected()
    {
        if (stream != null &&
            device != null)
        {
            return true;
        }

        try
        {
            DeviceList deviceList =
                DeviceList.Local;

            foreach (HidDevice candidate
                     in deviceList.GetHidDevices(
                         VendorId,
                         ProductId))
            {
                if (!candidate.TryOpen(
                        out HidStream? openedStream))
                {
                    continue;
                }

                lock (streamLock)
                {
                    device = candidate;
                    stream = openedStream;
                }

                ResetInputState();

                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private void ProcessReport(
        byte[] report,
        int length)
    {
        if (length <= 0)
            return;

        byte reportId =
            report[0];

        if (reportId == BluetoothReportId)
        {
            ProcessBluetoothExtendedReport(
                report,
                length);

            return;
        }

        if (reportId == UsbReportId)
        {
            ProcessReport01(
                report,
                length);

            return;
        }

        if (reportId == 0)
        {
            ProcessReportWithoutId(
                report,
                length);
        }
    }

    private void ProcessReport01(
        byte[] report,
        int length)
    {

        if (length >= 11)
        {
            byte leftX =
                report[1];

            byte leftY =
                report[2];

            byte buttons0 =
                report[8];

            byte buttons1 =
                report[9];

            byte buttons2 =
                report[10];

            ProcessUsbButtons(
                leftX,
                leftY,
                buttons0,
                buttons1,
                buttons2);

            return;
        }

        
        if (length >= 8)
        {
            byte leftX =
                report[1];

            byte leftY =
                report[2];

            byte buttons0 =
                report[5];

            byte buttons1 =
                report[6];

            byte buttons2 =
                report[7];

            ProcessBasicBluetoothButtons(
                leftX,
                leftY,
                buttons0,
                buttons1,
                buttons2);
        }
    }

    private void ProcessReportWithoutId(
        byte[] report,
        int length)
    {
       
        if (length < 10)
            return;

        byte leftX =
            report[0];

        byte leftY =
            report[1];

        byte buttons0 =
            report[7];

        byte buttons1 =
            report[8];

        byte buttons2 =
            report[9];

        ProcessUsbButtons(
            leftX,
            leftY,
            buttons0,
            buttons1,
            buttons2);
    }

    private void ProcessBluetoothExtendedReport(
        byte[] report,
        int length)
    {
        

        if (length < 12)
            return;

        byte leftX =
            report[2];

        byte leftY =
            report[3];

        byte buttons0 =
            report[9];

        byte buttons1 =
            report[10];

        byte buttons2 =
            report[11];

        ProcessUsbButtons(
            leftX,
            leftY,
            buttons0,
            buttons1,
            buttons2);
    }

    private void ProcessUsbButtons(
        byte leftX,
        byte leftY,
        byte buttons0,
        byte buttons1,
        byte buttons2)
    {
       
        DualSenseDirection analogDirection =
            GetAnalogDirection(
                leftX,
                leftY);

        DualSenseDirection dpadDirection =
            GetDpadDirection(
                buttons0);

        DualSenseDirection direction =
            dpadDirection != DualSenseDirection.None
                ? dpadDirection
                : analogDirection;

        UpdateDirection(
            direction);

        bool newCrossPressed =
            (buttons0 & 0x20) != 0;

        bool newCirclePressed =
            (buttons0 & 0x40) != 0;

        bool newSquarePressed =
            (buttons0 & 0x10) != 0;

        bool newTrianglePressed =
            (buttons0 & 0x80) != 0;

        
        bool newOptionsPressed =
            (buttons1 & 0x20) != 0;

        ProcessButtonEdge(
            newCrossPressed,
            ref crossPressed,
            DualSenseAction.Launch);

        ProcessButtonEdge(
            newCirclePressed,
            ref circlePressed,
            DualSenseAction.Back);

        ProcessButtonEdge(
            newSquarePressed,
            ref squarePressed,
            DualSenseAction.Import);

        ProcessButtonEdge(
            newTrianglePressed,
            ref trianglePressed,
            DualSenseAction.Remove);

        ProcessButtonEdge(
            newOptionsPressed,
            ref optionsPressed,
            DualSenseAction.Launch);
    }

    private void ProcessBasicBluetoothButtons(
        byte leftX,
        byte leftY,
        byte buttons0,
        byte buttons1,
        byte buttons2)
    {
        DualSenseDirection analogDirection =
            GetAnalogDirection(
                leftX,
                leftY);

        DualSenseDirection dpadDirection =
            GetDpadDirection(
                buttons0);

        DualSenseDirection direction =
            dpadDirection != DualSenseDirection.None
                ? dpadDirection
                : analogDirection;

        UpdateDirection(
            direction);

        bool newCrossPressed =
            (buttons0 & 0x20) != 0;

        bool newCirclePressed =
            (buttons0 & 0x40) != 0;

        bool newSquarePressed =
            (buttons0 & 0x10) != 0;

        bool newTrianglePressed =
            (buttons0 & 0x80) != 0;

        bool newOptionsPressed =
            (buttons1 & 0x20) != 0;

        ProcessButtonEdge(
            newCrossPressed,
            ref crossPressed,
            DualSenseAction.Launch);

        ProcessButtonEdge(
            newCirclePressed,
            ref circlePressed,
            DualSenseAction.Back);

        ProcessButtonEdge(
            newSquarePressed,
            ref squarePressed,
            DualSenseAction.Import);

        ProcessButtonEdge(
            newTrianglePressed,
            ref trianglePressed,
            DualSenseAction.Remove);

        ProcessButtonEdge(
            newOptionsPressed,
            ref optionsPressed,
            DualSenseAction.Launch);
    }

    private DualSenseDirection GetAnalogDirection(
        byte x,
        byte y)
    {
        double normalizedX =
            (x - 128.0) / 127.0;

        double normalizedY =
            (y - 128.0) / 127.0;

        double magnitude =
            Math.Sqrt(
                normalizedX * normalizedX +
                normalizedY * normalizedY);

       
        double threshold =
            currentDirection == DualSenseDirection.None
                ? EnterDeadzone
                : ExitDeadzone;

        if (magnitude < threshold)
        {
            return DualSenseDirection.None;
        }

        
        if (Math.Abs(normalizedX) >
            Math.Abs(normalizedY))
        {
            if (normalizedX > 0)
                return DualSenseDirection.Right;

            return DualSenseDirection.Left;
        }

        if (normalizedY > 0)
            return DualSenseDirection.Down;

        return DualSenseDirection.Up;
    }

    private DualSenseDirection GetDpadDirection(
        byte buttons0)
    {
        int hat =
            buttons0 & 0x0F;


        return hat switch
        {
            0 => DualSenseDirection.Up,

            1 => DualSenseDirection.Right,

            2 => DualSenseDirection.Right,

            3 => DualSenseDirection.Down,

            4 => DualSenseDirection.Down,

            5 => DualSenseDirection.Left,

            6 => DualSenseDirection.Left,

            7 => DualSenseDirection.Up,

            _ => DualSenseDirection.None
        };
    }

    private void UpdateDirection(
        DualSenseDirection newDirection)
    {
       
        if (newDirection == DualSenseDirection.None)
        {
            currentDirection =
                DualSenseDirection.None;

            lastDirection =
                DualSenseDirection.None;

            directionStartedAt =
                DateTime.MinValue;

            lastRepeatAt =
                DateTime.MinValue;

            return;
        }

        
        if (newDirection != currentDirection)
        {
            currentDirection =
                newDirection;

            lastDirection =
                newDirection;

            directionStartedAt =
                DateTime.UtcNow;

            lastRepeatAt =
                DateTime.UtcNow;

            TriggerDirection(
                newDirection);

            return;
        }

        DateTime now =
            DateTime.UtcNow;

        double heldMilliseconds =
            (now - directionStartedAt)
                .TotalMilliseconds;

        if (heldMilliseconds <
            InitialRepeatDelayMs)
        {
            return;
        }

        double repeatMilliseconds =
            (now - lastRepeatAt)
                .TotalMilliseconds;

        if (repeatMilliseconds <
            RepeatIntervalMs)
        {
            return;
        }

        lastRepeatAt =
            now;

        TriggerDirection(
            currentDirection);
    }

    private void TriggerDirection(
        DualSenseDirection direction)
    {
        switch (direction)
        {
            case DualSenseDirection.Up:
                TriggerAction(
                    DualSenseAction.Up);
                break;

            case DualSenseDirection.Down:
                TriggerAction(
                    DualSenseAction.Down);
                break;

            case DualSenseDirection.Left:
                TriggerAction(
                    DualSenseAction.Left);
                break;

            case DualSenseDirection.Right:
                TriggerAction(
                    DualSenseAction.Right);
                break;
        }
    }

    private void ProcessButtonEdge(
        bool isPressed,
        ref bool previousPressed,
        DualSenseAction action)
    {
        
        if (isPressed &&
            !previousPressed)
        {
            TriggerAction(action);
        }

        previousPressed =
            isPressed;
    }

    private void TriggerAction(
        DualSenseAction action)
    {
        try
        {
            ActionTriggered?.Invoke(
                this,
                action);
        }
        catch
        {
        }
    }

    private void ResetInputState()
    {
        currentDirection =
            DualSenseDirection.None;

        lastDirection =
            DualSenseDirection.None;

        directionStartedAt =
            DateTime.MinValue;

        lastRepeatAt =
            DateTime.MinValue;

        crossPressed = false;
        circlePressed = false;
        squarePressed = false;
        trianglePressed = false;
        optionsPressed = false;
    }

    private void CloseController()
    {
        lock (streamLock)
        {
            try
            {
                stream?.Close();
            }
            catch
            {
            }

            try
            {
                stream?.Dispose();
            }
            catch
            {
            }

            stream = null;
            device = null;
        }

        ResetInputState();
    }

    public void Dispose()
    {
        Stop();
    }
}