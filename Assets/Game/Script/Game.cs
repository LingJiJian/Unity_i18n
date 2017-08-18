using UnityEngine;
using System.Collections;
using LuaInterface;
using System;
using System.IO;

public class Game : MonoBehaviour 
{
    private static LuaState lua = null;  
    public static LuaState GetLuaState()
    {
        return lua;
    }

	void Start () 
    {  
    	new LuaResLoader();
        lua = new LuaState();                
        lua.Start();        

        string fullPath = Application.dataPath + "Game/Lua";
        lua.AddSearchPath(fullPath);      
        lua.Start();
        
        LuaBinder.Bind(lua);
        lua.DoFile("main.lua"); 
        lua.CheckTop();
        
    }

    void OnApplicationQuit()
    {
        lua.Dispose();
        lua = null;
    }
}
