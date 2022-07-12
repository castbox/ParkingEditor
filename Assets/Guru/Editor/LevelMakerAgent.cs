using System;
using System.Collections;
using System.Collections.Generic;
using BBG.BlockEscape;
using UnityEditor;
using UnityEngine;


/// <summary>
/// 外部调用的关卡编辑器助手
/// 获取Jenkins传入的各种参数
/// </summary>
public static class LevelMakerAgent 
{


    #region 环境变量解析

    private static string GetEnvironmentsString(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(value))
        {
            Debug.Log($"------- ENV {key}: string:{value}");
        }

        return value;
    }

    private static int GetEnvironmentsInt(string key)
    {
        var sval = GetEnvironmentsString(key);
        int value = 0;
        if (int.TryParse(sval, out value))
        {
            Debug.Log($"------- ENV {key}: int:{value}");
        }

        return value;
    } 
    
    private static bool GetEnvironmentsBool(string key)
    {
        var sval = GetEnvironmentsString(key);
        bool value = false;
        if (bool.TryParse(sval, out value))
        {
            Debug.Log($"------- ENV {key}: bool:{value}");
        }

        return value;
    } 
    

    #endregion



    #region Jenkins调用接口


    /// <summary>
    /// Jenkins 拉起操作
    /// </summary>
    public static void JenkinsBuild()
    {
        
        Debug.Log($"------------ START JENKINS BUILD ------------");
        int maxCount = 7;
        List<RemoteItemSettings> remoteItems = new List<RemoteItemSettings>(maxCount);
        
        for (int i = 0; i < maxCount; i++)
        {
            int id = i + 1;
            //  录入系统参数
            RemoteItemSettings rt = new RemoteItemSettings()
            {
                id = id,
                isActive = GetEnvironmentsBool($"IS_ACTIVE_{id}"),
                minMoves = GetEnvironmentsInt($"MIN_MOVES_{id}"),
                maxMoves = GetEnvironmentsInt($"MAX_MOVES_{id}"),
                numLevels = GetEnvironmentsInt($"NUM_LEVELS_{id}"),
                subFolder = GetEnvironmentsString($"SUB_FOLDER_{id}"),
                filenamePrefix = GetEnvironmentsString($"FILENAME_PREFIX_{id}"),
            };
            remoteItems.Add(rt);
        }
        // 注入Jenkins数据
        var levelSetting = Resources.Load<LevelCreatorSettings>(nameof(LevelCreatorSettings));
        if (levelSetting != null)
        {
            for (int i = 0; i < levelSetting.genItems.Count; i++)
            {
                var item = levelSetting.genItems[i];
                if (null != item && i < remoteItems.Count)
                {
                    remoteItems[i].FixData(ref item);
                }
            }
        }
        
        //  更改对应的元素
        EditorUtility.SetDirty(levelSetting);
        AssetDatabase.SaveAssets();
        
        LevelCreatorEditor.AutoBuildLevels(() =>
        {
            Debug.Log($"--------- All levels create over -------------");
        });
        
    }



    #endregion


    #region 单元测试


    
    [MenuItem("Test/load levelSettings")]
    private static void UnitTest()
    {
        
    }

    #endregion


}

#region 远程参数控制

[Serializable]
public class RemoteItemSettings
{
    public int id;
    public bool isActive;
    public int minMoves;
    public int maxMoves;
    public int numLevels;
    public string subFolder;
    public string filenamePrefix;


    public void FixData(ref LevelCreatorSettings.GenItem item)
    {
        item.isActive = isActive;
        item.minMoves = minMoves;
        item.maxMoves = maxMoves;
        item.numLevels = numLevels;
        item.subFolder = subFolder;
        item.filenamePrefix = filenamePrefix;
        item.expanded = true;
        Debug.Log($"---- Fix Data[{id}]  {item.minMoves}/{item.maxMoves }  : {item.numLevels}  -->  {item.subFolder}");
    }
    
    
    
}

#endregion




