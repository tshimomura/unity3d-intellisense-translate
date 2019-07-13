using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using HtmlAgilityPack;
using UnityEngine;
using UnityEditor;

public class XmlGenerator
{
    public static void Generate(string srcXmlPath, string dstXmlPath, string language, string referencePath)
    {
        if (!Directory.Exists(dstXmlPath))
        {
            Directory.CreateDirectory(dstXmlPath);
        }

        // 変換元のxmlファイルのリスト
        var srcXmlFiles = from file in Directory.EnumerateFiles(srcXmlPath, "*.xml", SearchOption.AllDirectories)
            where !file.Replace(Path.GetFileName(file), "").EndsWith(language + Path.DirectorySeparatorChar)
            select new
            {
                FullPath = file,
                RelativePath = file.Replace(srcXmlPath, "").Replace(Path.GetFileName(file), ""),
                FileName = Path.GetFileName(file)
            };

        int count = 1;
        int max = srcXmlFiles.Count();
        
        foreach (var f in srcXmlFiles)
        {
            string outputPath = dstXmlPath + f.RelativePath + "/" + language;

            if (!Directory.Exists(dstXmlPath + f.RelativePath))
            {
                Directory.CreateDirectory(dstXmlPath + f.RelativePath);
            }
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

//            Debug.Log(f);
            Translate(f.FullPath, outputPath + "/" + f.FileName, referencePath, count.ToString() + "/" + srcXmlFiles.Count().ToString());
            count++;
        }     
    }

    public static void Translate(string srcFile, string dstFile, string referencePath, string progressStr)
    {
        Hashtable replace_hash = new Hashtable
        {
            ["this"] = ".Index_operator",
            ["op_Equal"] = "-operator_eq",
            ["op_NotEqual"] = "-operator_ne",
            ["op_Plus"] = "-operator_add",
            ["op_Minus"] = "-operator_subtract",
            ["op_Multiply"] = "-operator_multiply",
            ["op_Divide"] = "-operator_divide",
            ["op_BitwiseOr"] = "-operator_bitwiseor",
            ["op_GreaterThan"] = "-operator_gt",
            ["op_LessThan"] = "-operator_ltr",
        };
        
        XmlDocument doc = new XmlDocument();
        doc.Load(srcFile);
        
        XmlNode root = doc.DocumentElement;
        XmlNodeList nodeList = root.SelectNodes("members/member");
        int count = 0;
        foreach (XmlNode member in nodeList)
        {
//            Debug.Log(member.Attributes["name"].Value);

            Regex r = new Regex(@"(UnityEngine|UnityEditor)\.(.*)", RegexOptions.IgnoreCase);
            Match m = r.Match(member.Attributes["name"].Value);
            if (m.Success)
            {
                string keyword = m.Groups[2].Value;
                string append = "";
                string filename = "";
                string clazz = "";
                string field;

                // XML内のメンバーに対応する、HTMLリファレンスのファイル名を決める
                if (keyword.Contains("."))
                {
                    r = new Regex(@"([\w\.]+)\.\#*([\w]*).*?(\`*(\d*))", RegexOptions.IgnoreCase);
                    m = r.Match(keyword);
                    clazz = m.Groups[1].Value;
                    field = m.Groups[2].Value;

                    if (m.Groups[3].Value.Length > 0)
                    {
                        append = "_" + m.Groups[4].Value;
                    }

                    if (replace_hash.ContainsKey(field))
                    {
                        filename = clazz + replace_hash[field];
                    }
                    else if (field == "op_Explicit")
                    {
                        filename = clazz + "-operator_" + clazz;
                    }
                    else if (field[0] == field.ToUpper()[0] || field == "iOS" || field == "tvOS")
                    {
                        filename = clazz + "." + field;
                    }
                    else
                    {
                        r = new Regex(@"implop_\w+\((\w+)\)", RegexOptions.IgnoreCase);
                        m = r.Match(keyword);
                        if (m.Success)
                        {
                            filename = clazz + "-operator_" + m.Groups[1].Value;
                        }
                        else
                        {
                            filename = clazz + "-" + field;
                        }
                    }
                }
                else
                {
                    filename = keyword.Replace("`", "_");
                }

                // 対応するファイルがリファレンス内に存在したらパース
                filename = referencePath + filename + append + ".html";
                if (File.Exists(filename))
                {
                    HtmlDocument html = new HtmlDocument();
                    html.Load(filename);
                    
                    // HTMLから説明,戻り値,パラメーターのいずれかのテキストを見つけたら、XMLの該当箇所を置換
                    HtmlNodeCollection nodes = html.DocumentNode.SelectNodes(@"//div[@id='content-wrap']/div/div/div[1]/div");
                    foreach (HtmlNode node in nodes)
                    {
                        HtmlNode n = node.SelectSingleNode(".//h2");

                        if (n != null)
                        {
                            XmlNode paraNode = member.SelectSingleNode("summary/para");

                            // XML内のmemberの該当箇所のテキストを日本語に差し替え
                            switch (n.InnerText)
                            {
                                case "説明":
                                    if (paraNode != null)
                                    {
                                        paraNode.InnerText = node.SelectSingleNode(".//p").InnerText;
                                    }
                                    break;
                                case "戻り値":
                                    if (paraNode != null)
                                    {
                                        string text = node.SelectSingleNode(".//p").InnerText.Trim();
                                        text = Regex.Replace(text, "[\r\n]", "");
                                        text = Regex.Replace(text, @"\s+", " ");
                                        paraNode.InnerText = text;
                                    }
                                    break;
                                case "パラメーター":
                                    HtmlNodeCollection param_nodes = node.SelectNodes(".//table/tr");
                                    foreach (HtmlNode param_node in param_nodes)
                                    {
                                        string param = param_node.SelectSingleNode(".//td[@class='name lbl']").InnerText;
                                        string desc = param_node.SelectSingleNode(".//td[@class='desc']").InnerText;
                                        foreach (XmlNode x in member.SelectNodes("param"))
                                        {
                                            if (x.Attributes["name"].Value == param)
                                            {
                                                x.InnerText = desc;
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    
                    EditorUtility.DisplayProgressBar(
                        "Mixing Reference XML files [" + progressStr + "]",
                        clazz,
                        count / (float) nodeList.Count
                    );
                }
            }

            count++;
        }

        doc.Save(dstFile);

        EditorUtility.ClearProgressBar();
    }
}
