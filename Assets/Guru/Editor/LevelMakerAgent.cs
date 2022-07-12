using System;
using System.Collections;
using System.Collections.Generic;
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
        List<RemoteItemSettings> settingItems = new List<RemoteItemSettings>(maxCount);
        
        for (int i = 0; i < 7; ++i)
        {
            //  录入系统参数
            RemoteItemSettings item = new RemoteItemSettings()
            {
                id = i,
                isActive = GetEnvironmentsBool($"IS_ACTIVE_{i}"),
                minMoves = GetEnvironmentsInt($"MIN_MOVES_{i}"),
                maxMoves = GetEnvironmentsInt($"MAX_MOVES_{i}"),
                numLevels = GetEnvironmentsInt($"NUM_LEVELS_{i}"),
                subFolder = GetEnvironmentsString($"SUB_FOLDER_{i}"),
                filenamePrifix = GetEnvironmentsString($"FILENAME_PREFIX_{i}"),
            };
            settingItems.Add(item);
        }
        //TODO 拉起构建窗口
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
    public string filenamePrifix;
}

#endregion




