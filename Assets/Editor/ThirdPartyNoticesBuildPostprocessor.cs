using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Unity 빌드가 끝난 뒤 ThirdPartyNotices.txt를 실행 파일과 같은 배포 폴더로 복사하는 책임을 가진다.
/// </summary>
public sealed class ThirdPartyNoticesBuildPostprocessor : IPostprocessBuildWithReport
{
    private const string SourcePath = "Assets/ThirdPartyNotices.txt";
    private const string DestinationFileName = "ThirdPartyNotices.txt";

    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report == null || string.IsNullOrWhiteSpace(report.summary.outputPath))
        {
            Debug.LogWarning("Third-party notices copy skipped because the build output path is missing.");
            return;
        }

        string sourceFullPath = Path.GetFullPath(SourcePath);
        if (!File.Exists(sourceFullPath))
        {
            Debug.LogWarning($"Third-party notices copy skipped because source file was not found: {sourceFullPath}");
            return;
        }

        string outputDirectory = GetBuildOutputDirectory(report.summary.outputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            Debug.LogWarning("Third-party notices copy skipped because the build output directory could not be resolved.");
            return;
        }

        Directory.CreateDirectory(outputDirectory);

        string destinationPath = Path.Combine(outputDirectory, DestinationFileName);
        File.Copy(sourceFullPath, destinationPath, true);
        Debug.Log($"Third-party notices copied to: {destinationPath}");
    }

    private static string GetBuildOutputDirectory(string outputPath)
    {
        if (Directory.Exists(outputPath))
        {
            return outputPath;
        }

        return Path.GetDirectoryName(outputPath);
    }
}
