using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
namespace Detect_xk
{
    class XmlTools
    {
        #region 读取XML配置文件
        /// <summary>
        /// 读取XML配置文件  失败返回null
        /// </summary>
        /// <param name="filePath">xml文件路径，相对于bin下debug目录</param>
        /// <returns>xml文档对象</returns>
        public static XmlDocument readXml(string filePath)
        {
            //获取可执行文件的路径-即bin目录下的debug或者release目录
            
            XmlDocument xml = new XmlDocument();
            //打开一个xml
            try
            {
                xml.Load(filePath);
                return xml;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
        #endregion

        #region 写入XML配置文件
        /// <summary>
                ///  写入XML配置文件 成功返回True 失败返回false
                /// </summary>
                /// <param name="xml">xml对象</param>
                /// <param name="filePath">文件路径</param>
                /// <returns></returns>
        public static Boolean writeXml(XmlDocument xml, string filePath)
        {
            try
            {
                xml.Save(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        #endregion

        #region 匹配 XPath 表达式的第一个 XmlNode
        /// <summary>
                /// 匹配 XPath 表达式的第一个 XmlNode
                /// </summary>
                /// <param name="xml">xml文档对象</param>
                /// <param name="xPath">xPath-路径匹配表达式</param>
                /// <returns>xml节点对象失败返回Null</returns>
        public static XmlNode getXmlNode(XmlDocument xml, string xPath)
        {
            //选择匹配 XPath 表达式的第一个 XmlNode
            XmlNode xmlNode = xml.SelectSingleNode(xPath);
            //读取节点数据
            if (xmlNode != null)
            {
                return xmlNode;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region 获取节点text
        /// <summary>
                /// 获取节点text
                /// </summary>
                /// <param name="xml获取节点textNode">节点对象</param>
                /// <returns>返回null则失败，返回""则代表节点内容为空，成功返回节点text</returns>
        public static string getNodeText(XmlNode xmlNode)
        {
            //读取节点数据
            if (xmlNode != null)
            {
                string nodeText = xmlNode.InnerText;
                if (nodeText != null)
                {
                    return nodeText;
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region 根据xPath获取节点个数
        /// <summary>
                /// 根据xPath获取节点个数
                /// </summary>
                /// <param name="xml">xml文档对象</param>
                /// <param name="xPath">xPath表达式</param>
                /// <returns>返回符合xPath的节点数，没有则返回0</returns>
        public static int getCountByXpath(XmlDocument xml, string xPath)
        {
            //读取节点list
            XmlNodeList nodelist = xml.SelectNodes(xPath);
            if (nodelist != null)
            {
                return nodelist.Count;
            }
            else
            {
                return 0;
            }
        }
        #endregion

        #region 根据xPath获取节点列表
        /// <summary>
                 ///  根据xPath获取节点列表
                 /// </summary>
                 /// <param name="xml">xml文档对象</param>
                 /// <param name="xPath">xPath表达式</param>
                 /// <returns>返回符合xPath的节点列表，失败返回null</returns>
        public static XmlNodeList getNodeListByXpath(XmlDocument xml, string xPath)
        {
            //读取节点list
            XmlNodeList nodelist = xml.SelectNodes(xPath);
            if (nodelist != null)
            {
                return nodelist;
            }
            else
            {
                return null;
            }
        }
        #endregion
        public static void createrXmlFile()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null));
            XmlElement root = xmlDoc.CreateElement("last_used");
            root.InnerText = "大屏001";
            xmlDoc.AppendChild(root);
            xmlDoc.Save(Environment.CurrentDirectory + "/config_load.xml");
        }
    }
}