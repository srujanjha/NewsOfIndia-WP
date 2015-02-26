using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

using Windows.UI.Xaml.Controls;
using Windows.UI.Text;
using System.Net;

namespace AppStudio.Controls.Html2Xaml
{
    public static class Html2XamlConverter
    {
        private static Dictionary<string, Dictionary<string, string>> attributes = new Dictionary<string, Dictionary<string, string>>();
        private static Dictionary<string, TagDefinition> tags = new Dictionary<string, TagDefinition>(){
			{"div", new TagDefinition("<Span{0}>", "</Span>", true)},
            {"img", new TagDefinition(parseImage){MustBeTop = true}},
            {"a", new TagDefinition(parseAnchor){MustBeTop = false}},
			{"p", new TagDefinition("<Paragraph LineStackingStrategy=\"MaxHeight\"{0}>", "</Paragraph><Paragraph />", true)},
			{"ul", new TagDefinition(parseList){MustBeTop = true}},
			{"b", new TagDefinition("<Bold{0}>")},
			{"i", new TagDefinition("<Italic{0}>")},         
			{"u", new TagDefinition("<Underline{0}>")},
			{"br", new TagDefinition("<LineBreak />", "")},
			{"table", new TagDefinition(parseTable){ MustBeTop = true}},
			{"blockquote", new TagDefinition("<Paragraph{0}>", "</Paragraph><Paragraph />", true)}
		};

        public static string Convert2Xaml(string HtmlString)
        {
            HtmlString = WebUtility.HtmlDecode(HtmlString).Replace("\"", "").Replace("&", "&amp;");
           
            populateAttributes();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(HtmlString);
            StringBuilder xamlString = new StringBuilder();

            foreach (var node in doc.DocumentNode.ChildNodes)
            {
                processTopNode(xamlString, node, true);
            }

            return xamlString.ToString();
        }

        private static void processTopNode(StringBuilder xamlString, HtmlNode node, bool isTop = false)
        {
            HtmlNode nextNode = null;
            if (!string.IsNullOrWhiteSpace(node.InnerText))
            {
                if (testTop(node.FirstChild))
                {
                    processTopNode(xamlString, node.FirstChild);
                    return;
                }
                if (node.Name.Equals("blockquote", StringComparison.CurrentCultureIgnoreCase) || node.Name.Equals("ul", StringComparison.CurrentCultureIgnoreCase) || node.Name.Equals("p", StringComparison.CurrentCultureIgnoreCase))
                {
                    nextNode = processNode(xamlString, node, true);
                }
                else
                {
                    writeBeginningTag(xamlString, tags["p"]);
                    nextNode = processNode(xamlString, node, true);
                    writeEndTag(xamlString, tags["p"]);
                }
            }

            if (nextNode != null)
            {
                processTopNode(xamlString, nextNode);
            }

            var imgNode = node.Descendants("img").FirstOrDefault();
            if (imgNode != null && imgNode.Name.Equals("img", StringComparison.CurrentCultureIgnoreCase))
            {
                writeBeginningTag(xamlString, tags["p"]);
                parseImage(xamlString, imgNode);
                writeEndTag(xamlString, tags["p"]);
            }

            if (!isTop && node.NextSibling != null)
            {

                if (testTop(node.NextSibling))
                    processTopNode(xamlString, node.NextSibling);
                else
                {
                    writeBeginningTag(xamlString, tags["p"]);
                    nextNode = processNode(xamlString, node.NextSibling);
                    writeEndTag(xamlString, tags["p"]);
                    if (nextNode != null)
                        processTopNode(xamlString, nextNode);
                }
            }
            //if (node.Name.Equals("img", StringComparison.CurrentCultureIgnoreCase))
            //{
            //    writeBeginningTag(xamlString, tags["p"]);
            //    parseImage(xamlString, node);
            //    writeEndTag(xamlString, tags["p"]);
            //}          
        }

        private static HtmlNode getNextTopNode(HtmlNode node)
        {
            if (node.NextSibling != null)
            {
                if (testTop(node.NextSibling))
                {
                    return node.NextSibling;
                }
            }

            if (node.ParentNode != node.OwnerDocument.DocumentNode && node.ParentNode.NextSibling != null)
            {
                if (testTop(node.ParentNode.NextSibling))
                {
                    return node.ParentNode.NextSibling;
                }
            }
            return null;
        }

        private static bool testTop(HtmlNode node)
        {
            if (node == null)
                return false;
            return (tags.ContainsKey(node.Name) && tags[node.Name].MustBeTop);
        }

        private static HtmlNode processNode(StringBuilder xamlString, HtmlNode node, bool isTop = false)
        {
            string tagName = node.Name.ToLower();

            HtmlNode top = null;
            if (tags.ContainsKey(tagName))
            {
                if (tags[tagName].MustBeTop && !isTop)
                    return node;

                if (tags[tagName].IsCustom)
                {
                    tags[tagName].CustomAction(xamlString, node);
                    //if (tagName != "a")
                    //{
                    //    return null;
                    //}
                }
                else
                {
                    writeBeginningTag(xamlString, tags[tagName]);

                    if (node.HasChildNodes)
                        top = processNode(xamlString, node.FirstChild);

                    writeEndTag(xamlString, tags[tagName]);
                }
            }
            else
            {
                if (node.NodeType == HtmlNodeType.Text)
                    xamlString.Append(node.InnerText);

                if (node.HasChildNodes)
                    top = processNode(xamlString, node.FirstChild);
            }

            if (top == null && node.NextSibling != null && !isTop)
                top = processNode(xamlString, node.NextSibling);

            return top;
        }

        private static void writeEndTag(StringBuilder xamlString, TagDefinition tag)
        {
            xamlString.Append(tag.EndXamlTag);
        }

        private static void writeBeginningTag(StringBuilder xamlString, TagDefinition tag)
        {
            string attrs = string.Empty;
            if (tag.Attributes.Count > 0)
                attrs = " " + string.Join(" ", tag.Attributes.Select(a => string.Format("{0}=\"{1}\"", a.Key, a.Value)).ToArray());

            xamlString.Append(string.Format(tag.BeginXamlTag, attrs));
        }

        private static void populateAttributes()
        {
            foreach (var attribute in attributes)
            {
                if (tags.ContainsKey(attribute.Key))
                    foreach (var attr in attribute.Value)
                        if (!tags[attribute.Key].Attributes.ContainsKey(attr.Key))
                            tags[attribute.Key].Attributes.Add(attr.Key, attr.Value);
            }
        }

        public static string Convert2Xaml(string HtmlString, Dictionary<string, Dictionary<string, string>> TagAttributes)
        {
            if (TagAttributes != null)
                attributes = TagAttributes;
            return Convert2Xaml(HtmlString);
        }
        private static void parseImage(StringBuilder xamlString, HtmlNode anchorNode)
        {

            xamlString.Append("<InlineUIContainer><Image");

            var navigateUrl = anchorNode.Attributes.FirstOrDefault(ha => ha.Name == "src");
            if (navigateUrl != null)
            {
                xamlString.AppendFormat(" Source=\"{0}\" ", navigateUrl.Value);
                xamlString.Append("Style=\"{StaticResource Html2XamlImageStyle}\">");
            }

            xamlString.Append("</Image></InlineUIContainer>");
        }

        private static void parseAnchor(StringBuilder xamlString, HtmlNode anchorNode)
        {
            xamlString.Append("<Hyperlink");
            var navigateUrl = anchorNode.Attributes.FirstOrDefault(ha => ha.Name == "href");
            if (navigateUrl != null)
            {
                SetUrl(xamlString, WebUtility.HtmlEncode(navigateUrl.Value));
            }
            xamlString.Append(" FontWeight=\"Bold\" >");
            xamlString.Append(anchorNode.InnerText);
            xamlString.Append("</Hyperlink>");
        }

        private static void SetUrl(StringBuilder xamlString, string url)
        {
            xamlString.AppendFormat(" NavigateUri=\"{0}\" ", url);
        }

        private static void parseList(StringBuilder xamlString, HtmlNode listNode)
        {
            foreach (var li in listNode.Descendants("li"))
            {
                xamlString.AppendFormat("<Paragraph>");
                processNode(xamlString, li.FirstChild);
                xamlString.AppendFormat("</Paragraph><Paragraph />");
            }
        }

        private static void parseTable(StringBuilder xamlString, HtmlNode tableNode)
        {
            xamlString.Append("<InlineUIContainer>");
            int currentRow = 0;
            int maxColumns = 0;
            CustomGrid tableGrid = new CustomGrid();
            TextBlock caption = null;
            var cap = tableNode.Descendants("caption").FirstOrDefault();
            if (cap != null)
            {
                caption = new TextBlock() { Text = cap.InnerText };
                currentRow += 1;
                tableGrid.Children.Add(caption);
                tableGrid.RowDefinitions.Add(new RowDefinition());
            }

            foreach (var row in tableNode.Descendants("tr"))
            {
                int colMax;
                tableGrid.RowDefinitions.Add(new RowDefinition());
                int currentColumn = 0;
                foreach (var headerCell in row.Descendants("th"))
                {
                    TextBlock cell = new TextBlock();
                    cell.FontWeight = FontWeights.Bold;
                    colMax = setCellAttributes(currentRow, currentColumn, headerCell, cell);
                    if (colMax > maxColumns)
                        maxColumns = colMax;
                    tableGrid.Children.Add(cell);
                    currentColumn += 1;
                }

                foreach (var cell in row.Descendants("td"))
                {
                    TextBlock textCell = new TextBlock();
                    colMax = setCellAttributes(currentRow, currentColumn, cell, textCell);
                    if (colMax > maxColumns)
                        maxColumns = colMax;
                    tableGrid.Children.Add(textCell);
                    currentColumn += 1;
                }
                currentRow += 1;
            }

            for (int xx = 0; xx <= maxColumns; xx++)
            {
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            if (maxColumns > 1 && caption != null)
                Grid.SetColumnSpan(caption, maxColumns);

            Dictionary<string, string> attr = new Dictionary<string, string>();
            if (attributes.ContainsKey("table"))
                attr = attributes["table"];
            string xaml = tableGrid.GetXaml(attr);
            xamlString.Append(xaml);
            xamlString.Append("</InlineUIContainer>");
        }

        private static int setCellAttributes(int currentRow, int currentColumn, HtmlNode cellNode, TextBlock cell)
        {
            int rowSpan = cellNode.GetAttributeValue("rowspan", 0);
            int colSpan = cellNode.GetAttributeValue("colspan", 0);
            if (rowSpan > 0)
            {
                Grid.SetRowSpan(cell, rowSpan);
            }
            if (colSpan > 0)
            {
                Grid.SetColumnSpan(cell, colSpan);
            }
            if (currentRow > 0)
            {
                Grid.SetRow(cell, currentRow);
            }
            if (currentColumn > 0)
            {
                Grid.SetColumn(cell, currentColumn);
            }
            cell.Text = cellNode.InnerText;

            return colSpan + currentColumn;
        }
    }
}
