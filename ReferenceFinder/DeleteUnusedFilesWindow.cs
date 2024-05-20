using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Editor.ReferenceFinder_master.ReferenceFinder
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public class DeleteUnusedFilesWindow : EditorWindow
    {
        private string filePath = "";
        private string targetFolder = "/Scene/";

        private static ReferenceFinderData data = new ReferenceFinderData();

        [MenuItem("Window/Delete Unused Files in Folders")]
        public static void ShowWindow()
        {
            // 显示存在的窗口实例。如果没有，则创建一个。  
            EditorWindow.GetWindow(typeof(DeleteUnusedFilesWindow));
        }

        void OnGUI()
        {
            // 标题栏  
            GUILayout.Label("Delete Unused Files", EditorStyles.boldLabel);

            // 文件路径输入框  
            filePath = EditorGUILayout.TextField("File Path:", filePath);
            if (GUILayout.Button("Select File"))
            {
                filePath = EditorUtility.OpenFilePanel("Select need_delete_fbx.txt", "", "txt");
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = ""; // 用户取消选择，清空路径  
                }
            }

            // 目标文件夹输入框  
            targetFolder = EditorGUILayout.TextField("Target Folder:", targetFolder);

            // 删除按钮  
            if (GUILayout.Button("Delete Files"))
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    data.DeleteFilesRecursive(filePath, targetFolder);
                }
                else
                {
                    Debug.LogError("File not found: " + filePath);
                }
            }
        }
    }
}
