using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LuaInterface;

public class UIi18n : MonoBehaviour {

	public string key;
	public Text text;

	void Start()
	{	
		LuaTable i18n = Game.GetLuaState().GetTable ("i18n");
		if(i18n[key] != null){
			text.text = i18n[key].ToString();
		}
	}
}
