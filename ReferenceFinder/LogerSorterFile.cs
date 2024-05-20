using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;

public static class LogerSorterFile
{

    private const string NEED_DELETE_CATALOGUE = "needDeleteCatalogue";
    private const string DEPENDENCED_INFO_CATALOGUE = "dependencedInfoCatalogue";

    private const string NEED_DELETE_FILE_PERFIX = "need_delete_";
    private const string DEPENDENCED_INFO_FILE_PERFIX = "dependenced_";

    public static void LogToFileDependenced(ref Dictionary<string, ReferenceFinderData.AssetDescription> assetDict)
    {
        Dictionary<string, AssetDescription> classifiedFiles = new Dictionary<string, AssetDescription>();
        classifiedFiles.Clear();

        int i = 0;
        foreach (var pair in assetDict)
        {
            string path = pair.Value.path;
            string extension = GetFileNameExtention(path); // 获取扩展名并转为小写  
            if (!classifiedFiles.ContainsKey(extension))
            {
                classifiedFiles[extension] = new AssetDescription();
            }
            classifiedFiles[extension].path_list.Add(path);

            i++;
            if ((i % 100 == 0) && EditorUtility.DisplayCancelableProgressBar("Search Dependenced", string.Format("Searching {0} assets", i), (float)i / assetDict.Count))
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            foreach (var pair2 in assetDict)
            {
                foreach (string item in pair2.Value.dependencies)
                {
                    if (item == path)
                    {
                        if (!classifiedFiles[extension].depend_info.ContainsKey(path))
                        {
                            classifiedFiles[extension].depend_info[path] = new DepAssetDescription();
                        }
                        classifiedFiles[extension].depend_info[path].depend_path_list.Add(pair2.Value.path);
                        break;
                    }
                }
                foreach (string item in pair2.Value.references)
                {
                    if (item == path)
                    {
                        if (!classifiedFiles[extension].depend_info.ContainsKey(path))
                        {
                            classifiedFiles[extension].depend_info[path] = new DepAssetDescription();
                        }
                        classifiedFiles[extension].depend_info[path].reference_path_list.Add(pair2.Value.path);
                        break;
                    }
                }
            }
        }

        LogerDistribute(ref classifiedFiles);
        
        classifiedFiles.Clear();
    }

    private static void LogerDistribute(ref Dictionary<string, AssetDescription> classifiedFiles)
    {
        if (!Directory.Exists(NEED_DELETE_CATALOGUE))
        {
            Directory.CreateDirectory(NEED_DELETE_CATALOGUE);
        }
        if (!Directory.Exists(DEPENDENCED_INFO_CATALOGUE))
        {
            Directory.CreateDirectory(DEPENDENCED_INFO_CATALOGUE);
        }

        // 遍历每个分类并获取大小、排序、写入文件  
        int j = 0;
        foreach (var kvp in classifiedFiles)
        {
            string extension = kvp.Key;

            j++;
            EditorUtility.DisplayProgressBar("Log to File", "Log to file", (float)j / classifiedFiles.Count);

            // 根据文件大小排序  
            kvp.Value.path_list.Sort((a, b) => GetFileSize(b).CompareTo(GetFileSize(a)));

            // 写入到文件  
            string del_outputFilePath = $"{NEED_DELETE_CATALOGUE}/{NEED_DELETE_FILE_PERFIX}{extension.Trim('.')}.txt";
            StreamWriter del_sw = new StreamWriter(del_outputFilePath);

            string dep_outputFilePath = $"{DEPENDENCED_INFO_CATALOGUE}/{DEPENDENCED_INFO_FILE_PERFIX}{extension.Trim('.')}.txt";
            StreamWriter dep_sw = new StreamWriter(dep_outputFilePath);

            foreach (var filePathItem in kvp.Value.path_list)
            {
                if (kvp.Value.needDelete(filePathItem))
                {
                    string size = GetFileSize(filePathItem).ToString("N0"); // 格式化大小，没有小数位  
                    del_sw.WriteLine($"{filePathItem} ({size} bytes)");
                }
                else
                {
                    string size = GetFileSize(filePathItem).ToString("N0"); // 格式化大小，没有小数位  
                    dep_sw.WriteLine($"{filePathItem} ({size} bytes)");
                    foreach (string item in kvp.Value.depend_info[filePathItem].depend_path_list)
                    {
                        dep_sw.WriteLine("  [dependencied]" + item + "、");
                    }
                    foreach (string item in kvp.Value.depend_info[filePathItem].reference_path_list)
                    {
                        dep_sw.WriteLine("  [referenced]" + item + "、");
                    }
                    dep_sw.WriteLine();
                }
            }

            del_sw.Close();
            dep_sw.Close();
        }

        EditorUtility.ClearProgressBar();
    }

    public static void DeleteFilesRecursive(string filePath, string targetFolder)
    {
        string realFilePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "needDeleteCatalogue", filePath);
        string[] lines = File.ReadAllLines(realFilePath);

        int count = 0;

        foreach (string line in lines)
        {
            // 假设每行都是完整的文件路径和大小（带括号和空格）  
            // 我们需要去掉括号和大小信息，只保留文件路径  
            int index = line.IndexOf('(');
            if (index != -1)
            {
                string filePathWithoutSize = line.Substring(0, index).Trim(); // 去除括号和空格  

                // 检查路径是否包含 "Scene" 目录  
                if (filePathWithoutSize.Contains(targetFolder))
                {
                    File.Delete(filePathWithoutSize);
                    UnityEngine.Debug.Log("Deleted: " + filePathWithoutSize);
                    count++;
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Invalid line format: " + line);
            }
        }

        UnityEngine.Debug.Log("Deleted Num: " + count);
    }

    // 获取扩展名并转为小写  
    private static string GetFileNameExtention(string filePath)
    {
        return Path.GetExtension(filePath).ToLower(); 
    }

    // 获取文件大小（以字节为单位）  
    private static long GetFileSize(string filePath)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        return fileInfo.Length;
    }

    private class DepAssetDescription
    {
        public List<string> depend_path_list = new List<string>();
        public List<string> reference_path_list = new List<string>();

        public bool needDelete()
        {
            return depend_path_list.Count == 0 && reference_path_list.Count == 0;
        }
    }


    private class AssetDescription
    {
        public List<string> path_list = new List<string>();
        public Dictionary<string, DepAssetDescription> depend_info = new Dictionary<string, DepAssetDescription>();

        public bool needDelete(string path)
        {
            return !depend_info.ContainsKey(path);
        }
    }
}