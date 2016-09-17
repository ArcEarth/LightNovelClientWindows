using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace LightNovel.Data
{
    internal static class HtmlNodeExtension
    {
        public static bool HasClass(this HtmlNode node, string name)
        {
            return node.Attributes["class"] != null && node.Attributes["class"].Value.Contains(name);
        }

        public static HtmlNode PreviousSiblingElement(this HtmlNode node, string name = null)
        {
            node = node.PreviousSibling;
            if (name == null)
            {
                while (node != null && node.NodeType != HtmlNodeType.Element)
                    node = node.PreviousSibling;
                return node;
            }
            else
            {
                while (node != null && (node.NodeType != HtmlNodeType.Element || node.Name != name))
                    node = node.PreviousSibling;
                return node;
            }
        }
        public static HtmlNode NextSiblingElement(this HtmlNode node, string name = null)
        {
            node = node.NextSibling;
            if (name == null)
            {
                while (node != null && node.NodeType != HtmlNodeType.Element)
                    node = node.NextSibling;
                return node;
            }
            else
            {
                while (node != null && (node.NodeType != HtmlNodeType.Element || node.Name != name))
                    node = node.NextSibling;
                return node;
            }
        }
        public static HtmlNode NextSiblingClass(this HtmlNode node, string classname)
        {
            while (node != null && (node.NodeType != HtmlNodeType.Element || node.HasClass(classname)))
                node = node.NextSibling;
            return node;
        }
        public static HtmlNode PreviousSiblingClass(this HtmlNode node, string classname)
        {
            while (node != null && (node.NodeType != HtmlNodeType.Element || node.HasClass(classname)))
                node = node.PreviousSibling;
            return node;
        }
        public static HtmlNode FirstChildClass(this HtmlNode node, string classname)
        {
            node = node.FirstChild;
            while (node != null && (node.NodeType != HtmlNodeType.Element || !node.HasClass(classname)))
                node = node.NextSibling;
            return node;
        }
        public static HtmlNode FirstDescendantClass(this HtmlNode node, string classname)
        {
           return node.Descendants().FirstOrDefault(c => c.HasClass(classname));
        }
    }
}
