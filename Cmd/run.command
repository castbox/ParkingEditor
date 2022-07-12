#!/bin/bash

#================= 环境变量定义 =================

export IS_ACTIVE_1=True
export MIN_MOVES_1=3
export MAX_MOVES_1=4
export NUM_LEVELS_1=101
export SUB_FOLDER_1=1_beginner
export FILENAME_PREFIX_1=''

export IS_ACTIVE_2=True
export MIN_MOVES_2=5
export MAX_MOVES_2=6
export NUM_LEVELS_2=200
export SUB_FOLDER_2=2_easy
export FILENAME_PREFIX_2=''

export IS_ACTIVE_3=True
export MIN_MOVES_3=7
export MAX_MOVES_3=9
export NUM_LEVELS_3=200
export SUB_FOLDER_3=3_medium
export FILENAME_PREFIX_3=''

export IS_ACTIVE_4=True
export MIN_MOVES_4=10
export MAX_MOVES_4=15
export NUM_LEVELS_4=200
export SUB_FOLDER_4=4_hard
export FILENAME_PREFIX_4=''

export IS_ACTIVE_5=True
export MIN_MOVES_5=16
export MAX_MOVES_5=22
export NUM_LEVELS_5=200
export SUB_FOLDER_5=5_veryhard
export FILENAME_PREFIX_5=''

export IS_ACTIVE_6=True
export MIN_MOVES_6=23
export MAX_MOVES_6=33
export NUM_LEVELS_6=200
export SUB_FOLDER_6=6_expert
export FILENAME_PREFIX_6=''

export IS_ACTIVE_7=True
export MIN_MOVES_7=30
export MAX_MOVES_7=50
export NUM_LEVELS_7=100
export SUB_FOLDER_7=7_hell
export FILENAME_PREFIX_7=''

#================= 环境变量定义 =================


UNITY_APP=/Applications/Unity/Hub/Editor/2020.3.2f1c1/Unity.app/Contents/MacOS/Unity
METHOD=LevelMakerAgent.JenkinsBuild
WORKSPACE=/Users/eric/Workspace/Castbox/Parking/ParkingEditor

# 执行操作
$UNITY_APP -buildTarget android -batchmode -executeMethod ${METHOD} -logFile $WORKSPACE/unity3d_editor.log

echo "--------- Build is Over -----------"