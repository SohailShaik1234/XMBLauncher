using System;
using System.Diagnostics;
using System.IO;
using XMBLauncher.Models;

namespace XMBLauncher.Services;

public class GameImportService
{
    public GameExecutableInfo ReadExecutable(
        string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException(
                "Executable path cannot be empty.");
        }

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                "The executable could not be found.",
                executablePath);
        }

        if (!string.Equals(
                Path.GetExtension(executablePath),
                ".exe",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The selected file is not an EXE file.");
        }

        FileVersionInfo versionInfo =
            FileVersionInfo.GetVersionInfo(
                executablePath);

        return new GameExecutableInfo
        {
            ExecutablePath = executablePath,

            FileName =
                Path.GetFileName(executablePath),

            FileNameWithoutExtension =
                Path.GetFileNameWithoutExtension(
                    executablePath),

            Directory =
                Path.GetDirectoryName(
                    executablePath) ?? "",

            ProductName =
                versionInfo.ProductName ?? "",

            CompanyName =
                versionInfo.CompanyName ?? "",

            FileVersion =
                versionInfo.FileVersion ?? "",

            Description =
                versionInfo.FileDescription ?? ""
        };
    }
}