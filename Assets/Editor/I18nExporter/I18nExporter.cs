/****************************************************************************
Copyright (c) 2015 Lingjijian

Created by Lingjijian on 2015

342854406@qq.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
****************************************************************************/
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Xml;
// 国际化 导出
public class I18nExporter : Editor
{
    private static Dictionary<string, Dictionary<string, string>> _markDatas;
    private static Dictionary<string, string> _markFiles;
	private static int _markId;
    private static List<string> _ignorePath;
    private static string _outputPath;
	private static string _scanDir;
    private static HashSet<int> _markIdMap;

    [MenuItem("Tools/I18n Export")]
    public static void Run()
    {
        Load();
        ScanCode();
		ScanPrefab();
        Export();
    }

    private static void Load()
    {
		_outputPath = Application.dataPath + "/Lua/Common/i18n.lua";
		_scanDir = Application.dataPath + "/Lua";
		_ignorePath = new List<string>()
		{
			"i18n.lua"
		};

		TextAsset textAsset = (TextAsset)AssetDatabase.LoadAssetAtPath("Asset/Editor/I18nExporter/config.txt",typeof(TextAsset));
        if (textAsset)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(textAsset.text);    //加载Xml文件
			XmlElement rootElem = doc.DocumentElement;   //获取根节点
			XmlNodeList outPaths = rootElem.GetElementsByTagName("OutPath");
			_outputPath = Application.dataPath + outPaths [0].InnerText;
			XmlNodeList scanDir = rootElem.GetElementsByTagName ("ScanDir");
			_scanDir = Application.dataPath + scanDir[0].InnerText;
			XmlNodeList ignorePaths = rootElem.GetElementsByTagName ("IgnorePath");
			_ignorePath = new List<string> ();
			for (int i = 0; i < ignorePaths.Count; i++) {
				_ignorePath.Add (ignorePaths [i].InnerText);
			}
        }

        if (File.Exists(_outputPath))
        {
            _markDatas = new Dictionary<string, Dictionary<string, string>>();
            _markFiles = new Dictionary<string, string>();
            _markIdMap = new HashSet<int>();

            StreamReader sr = new StreamReader(_outputPath, Encoding.UTF8);
            string line;
            Dictionary<string, string> markDic = null;
            while ((line = sr.ReadLine()) != null)
            {
				if (line.StartsWith ("i18n[")) {
					int startIdx = line.LastIndexOf ("i18n[\"") + 6;
					int endIdx = line.IndexOf ("\"]");
					string key = line.Substring (startIdx, endIdx - startIdx);

					startIdx = line.LastIndexOf ("= \"") + 3;
					endIdx = line.LastIndexOf ("\"");
					string value = line.Substring (startIdx, endIdx - startIdx);
					markDic.Add (string.Format ("i18n[\"{0}\"]", key), value);

                    int n; //只存数字键
                    if(int.TryParse(key,out n))
                    {
					   _markIdMap.Add (n);
                    }
				} else if (line.StartsWith ("--# ") && line.EndsWith(".lua")) {
					int startIdx = line.IndexOf ("--# ") + 4;
					int endIdx = line.LastIndexOf (".lua") + 4;
					string fileName = line.Substring (startIdx, endIdx - startIdx);

					_markDatas.Add (fileName, new Dictionary<string, string> ());
					markDic = _markDatas [fileName];
				} else if (line.StartsWith ("--# ") && line.EndsWith(".prefab")) {
					int startIdx = line.IndexOf ("--# ") + 4;
					int endIdx = line.LastIndexOf (".prefab") + 7;
					string fileName = line.Substring (startIdx, endIdx - startIdx);

					_markDatas.Add (fileName, new Dictionary<string, string> ());
					markDic = _markDatas [fileName];
				}
            }
            sr.Close();
            sr.Dispose();
        }
        else
        {
            _markDatas = new Dictionary<string, Dictionary<string, string>>();
            _markFiles = new Dictionary<string, string>();
            _markIdMap = new HashSet<int>();
            _markId = 1000;
        }
    }

    private static void ScanCode()
    {
		Helper.forEachHandle(_scanDir, new List<string> { "lua" }, (string filePath) =>
        {
            string basePath = Path.GetFileName(filePath);
            if (_ignorePath.Contains(basePath)) return;

            Dictionary<string, string> markDic = null; //标记字典
            if (!_markDatas.ContainsKey(basePath))
            {
                markDic = new Dictionary<string, string>();
                _markDatas.Add(basePath, markDic);
            }
            else
            {
                markDic = _markDatas[basePath];
            }

            using (StreamReader sr = new StreamReader(filePath, Encoding.UTF8))
            {
                string line;
                StringBuilder content = new StringBuilder();
                while ((line = sr.ReadLine()) != null)
                {
                    StringBuilder newLine = new StringBuilder();
                    StringBuilder markKey = new StringBuilder();
                    bool isMarking = false;
                    char[] chars = line.ToCharArray();
                    for (int i = 0; i < chars.Length; i++)
                    {
                        char c = chars[i];
                        bool _isReset = false;
                        if (c == '"' && chars[i - 1] != '\\')
                        {
                            isMarking = !isMarking;
                            if (isMarking == false) //结束标记
                            {
                                string markKeyString = markKey.ToString();
                                
                                if (isChinese(markKeyString))
                                {
                                    string holder = makeAddHolder(basePath, markKeyString);

                                    newLine.Append(holder);
                                    _isReset = true;
                                }
                                else //不是中文
                                {
                                    newLine.Append('"').Append(markKeyString);
                                }
                                markKey = new StringBuilder(); //清空
                            }
                        }
                        else
                        {
                            if (isMarking)
                            {
                                markKey.Append(c);
                            }
                        }
                        if (!isMarking && !_isReset)
                        {
                            newLine.Append(c);
                        }
                    }
                    content.Append(newLine).Append('\n');
                }

                if (markDic.Count > 0)
                {
                    _markFiles.Add(filePath, content.ToString());
                }
                sr.Close();
            }
        });
    }

	private static void ScanPrefab()
	{
        string prefabDir = Application.dataPath + "/Game/Resources/Prefab";
        Helper.forEachHandle(prefabDir, new List<string> { "prefab" }, (string path) =>
        {
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace(Application.dataPath, "Assets"));
            if (obj != null)
            {
				string basename = path.Replace(Application.dataPath,"");
                Text[] textComps = obj.GetComponentsInChildren<Text>(true);
                foreach (Text textComp in textComps)
                {
					if(isChinese(textComp.text)){
						UIi18n i18n = textComp.gameObject.GetComponent<UIi18n>();
						if(i18n == null){
							i18n = textComp.gameObject.AddComponent<UIi18n>();
							_markId = getNextMarkId();
							i18n.key = _markId.ToString();
                            i18n.text = textComp;
                            string content = textComp.text.Replace("\n","\\n").Replace("\r","").Replace("\"","\\\"");
							
							Dictionary<string,string> markDic;
							if(_markDatas.ContainsKey(basename)){
								markDic = _markDatas[basename];
							}else{
								markDic = new Dictionary<string, string>();
								_markDatas.Add(basename,markDic);
							}
							markDic.Add(string.Format("i18n[\"{0}\"]",_markId),content);
						}
					}
                }
            }
        });

	}

    private static void Export()
    {
        StringBuilder i18n = new StringBuilder();
        i18n.Append("local i18n = {}\n");

        foreach (string fileName in _markDatas.Keys)
        {
            if(_markDatas[fileName].Count > 0)
            {
                i18n.AppendFormat("--# {0}\n", fileName);
            }
			foreach(string holder in _markDatas[fileName].Keys)
            {
				i18n.AppendFormat("{0} = \"{1}\"\n",holder, _markDatas[fileName][holder]);
            }
        }
        //i18n.Append("\nLDeclare(\"i18n\", i18n)\n");
        i18n.Append("return i18n\n");

        using (FileStream fs = new FileStream(_outputPath, FileMode.Create))
        {
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(i18n.ToString());
            sw.Flush();
            sw.Close();
            fs.Close();
        }

        foreach (string filePath in _markFiles.Keys)
        {
			if (filePath.EndsWith (".lua")) {
				using (FileStream fs = new FileStream (filePath, FileMode.Create)) {
					StreamWriter sw = new StreamWriter (fs);
					sw.Write (_markFiles [filePath]);
					sw.Flush ();
					sw.Close ();
					fs.Close ();
				}  
			}
        }

        AssetDatabase.Refresh();
        Debug.Log("导出完成 " + _outputPath);
    }

    private static string makeAddHolder(string fileName ,string chinese)
    {
        Dictionary<string, string> dic = _markDatas[fileName];

		string holder = string.Format ("i18n[\"{0}\"]", getNextMarkId ().ToString ());
		dic.Add (holder, chinese);
		return holder;
    }

    private static int getNextMarkId()
    {
		int curMarkId = 1000;
        List<int> sortList = new List<int> ();
        foreach (int i in _markIdMap) {
            sortList.Add (i);
        }
        sortList.Sort ();

        for(int i = 1000;i <= 99999 ;i++){
            if(!sortList.Contains(i)){
                curMarkId = i;
                break;
            }
        }
        _markIdMap.Add (curMarkId);
        return curMarkId;
    }

    private static bool isChinese(string text)
    {
        bool hasChinese = false;
        char[] c = text.ToCharArray();
        int len = c.Length;
        for (int i = 0; i < len; i++)
        {
            if (c[i] >= 0x4e00 && c[i] <= 0x9fbb)
            {
                hasChinese = true;
                break;
            }
        }
        return hasChinese;
    }
}
