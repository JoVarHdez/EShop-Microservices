using System.Diagnostics;
namespace Shopping.Web.Razor.Tests.Support;

public static class DockerAvailability
{
    public static bool IsDockerAvailable()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "info",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void EnsureDockerOrFail(string reason)
    {
        if (!IsDockerAvailable())
        {
            throw new InvalidOperationException(reason);
        }
    }
}
