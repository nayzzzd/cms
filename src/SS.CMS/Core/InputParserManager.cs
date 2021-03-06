using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Datory.Utils;
using SS.CMS.Abstractions;
using SS.CMS.Abstractions.Parse;

namespace SS.CMS.Core
{
    public class InputParserManager
    {
        private readonly IPathManager _pathManager;

        public InputParserManager(IPathManager pathManager)
        {
            _pathManager = pathManager;
        }

        public async Task<string> GetContentByTableStyleAsync(string content, string separator, Config config, Site site, TableStyle style, string formatString, NameValueCollection attributes, string innerHtml, bool isStlEntity)
        {
            var parsedContent = content;

            var inputType = style.InputType;

            if (inputType == InputType.Date)
            {
                var dateTime = TranslateUtils.ToDateTime(content);
                if (dateTime != Constants.SqlMinValue)
                {
                    if (string.IsNullOrEmpty(formatString))
                    {
                        formatString = DateUtils.FormatStringDateOnly;
                    }
                    parsedContent = DateUtils.Format(dateTime, formatString);
                }
                else
                {
                    parsedContent = string.Empty;
                }
            }
            else if (inputType == InputType.DateTime)
            {
                var dateTime = TranslateUtils.ToDateTime(content);
                if (dateTime != Constants.SqlMinValue)
                {
                    if (string.IsNullOrEmpty(formatString))
                    {
                        formatString = DateUtils.FormatStringDateTime;
                    }
                    parsedContent = DateUtils.Format(dateTime, formatString);
                }
                else
                {
                    parsedContent = string.Empty;
                }
            }
            else if (inputType == InputType.CheckBox || inputType == InputType.Radio || inputType == InputType.SelectMultiple || inputType == InputType.SelectOne)//选择类型
            {
                var selectedTexts = new List<string>();
                var selectedValues = Utilities.GetStringList(content);
                var styleItems = style.Items;
                if (styleItems != null)
                {
                    foreach (var itemInfo in styleItems)
                    {
                        if (selectedValues.Contains(itemInfo.Value))
                        {
                            selectedTexts.Add(isStlEntity ? itemInfo.Value : itemInfo.Label);
                        }
                    }
                }
                
                parsedContent = separator == null ? Utilities.ToString(selectedTexts) : Utilities.ToString(selectedTexts, separator);
            }
            //else if (style.InputType == InputType.TextArea)
            //{
            //    parsedContent = StringUtils.ReplaceNewlineToBR(parsedContent);
            //}
            else if (inputType == InputType.TextEditor)
            {
                parsedContent = await _pathManager.DecodeTextEditorAsync(site, parsedContent, true);
            }
            else if (inputType == InputType.Image)
            {
                parsedContent = await GetImageOrFlashHtmlAsync(site, parsedContent, attributes, isStlEntity);
            }
            else if (inputType == InputType.Video)
            {
                parsedContent = await GetVideoHtmlAsync(config, site, parsedContent, attributes, isStlEntity);
            }
            else if (inputType == InputType.File)
            {
                parsedContent = GetFileHtmlWithoutCount(config, site, parsedContent, attributes, innerHtml, isStlEntity, false, false);
            }

            return parsedContent;
        }

        public async Task<string> GetContentByTableStyleAsync(Content content, string separator, Config config, Site site, TableStyle style, string formatString, int no, NameValueCollection attributes, string innerHtml, bool isStlEntity)
        {
            var value = content.Get<string>(style.AttributeName);
            var parsedContent = string.Empty;

            var inputType = style.InputType;

            if (inputType == InputType.Date)
            {
                var dateTime = TranslateUtils.ToDateTime(value);
                if (dateTime != Constants.SqlMinValue)
                {
                    if (string.IsNullOrEmpty(formatString))
                    {
                        formatString = DateUtils.FormatStringDateOnly;
                    }
                    parsedContent = DateUtils.Format(dateTime, formatString);
                }
            }
            else if (inputType == InputType.DateTime)
            {
                var dateTime = TranslateUtils.ToDateTime(value);
                if (dateTime != Constants.SqlMinValue)
                {
                    if (string.IsNullOrEmpty(formatString))
                    {
                        formatString = DateUtils.FormatStringDateTime;
                    }
                    parsedContent = DateUtils.Format(dateTime, formatString);
                }
            }
            else if (inputType == InputType.CheckBox || inputType == InputType.Radio || inputType == InputType.SelectMultiple || inputType == InputType.SelectOne)//选择类型
            {
                var selectedTexts = new List<string>();
                var selectedValues = Utilities.GetStringList(value);
                var styleItems = style.Items;
                if (styleItems != null)
                {
                    foreach (var itemInfo in styleItems)
                    {
                        if (selectedValues.Contains(itemInfo.Value))
                        {
                            selectedTexts.Add(isStlEntity ? itemInfo.Value : itemInfo.Label);
                        }
                    }
                }
                
                parsedContent = separator == null ? Utilities.ToString(selectedTexts) : Utilities.ToString(selectedTexts, separator);
            }
            else if (inputType == InputType.TextEditor)
            {
                parsedContent = await _pathManager.DecodeTextEditorAsync(site, value, true);
            }
            else if (inputType == InputType.Image)
            {
                if (no <= 1)
                {
                    parsedContent = await GetImageOrFlashHtmlAsync(site, value, attributes, isStlEntity);
                }
                else
                {
                    var extendName = ColumnsManager.GetExtendName(style.AttributeName, no - 1);
                    var extend = content.Get<string>(extendName);
                    parsedContent = await GetImageOrFlashHtmlAsync(site, extend, attributes, isStlEntity);
                }
            }
            else if (inputType == InputType.Video)
            {
                if (no <= 1)
                {
                    parsedContent = await GetVideoHtmlAsync(config, site, value, attributes, isStlEntity);
                }
                else
                {
                    var extendName = ColumnsManager.GetExtendName(style.AttributeName, no - 1);
                    var extend = content.Get<string>(extendName);
                    parsedContent = await GetVideoHtmlAsync(config, site, extend, attributes, isStlEntity);
                }
            }
            else if (inputType == InputType.File)
            {
                if (no <= 1)
                {
                    parsedContent = GetFileHtmlWithoutCount(config, site, value, attributes, innerHtml, isStlEntity, false, false);
                }
                else
                {
                    var extendName = ColumnsManager.GetExtendName(style.AttributeName, no - 1);
                    var extend = content.Get<string>(extendName);
                    parsedContent = GetFileHtmlWithoutCount(config, site, extend, attributes, innerHtml, isStlEntity, false, false);
                }
            }
            else
            {
                parsedContent = value;
            }

            return parsedContent;
        }

        public async Task<string> GetImageOrFlashHtmlAsync(Site site, string imageUrl, NameValueCollection attributes, bool isStlEntity)
        {
            var retVal = string.Empty;
            if (!string.IsNullOrEmpty(imageUrl))
            {
                imageUrl = await _pathManager.ParseNavigationUrlAsync(site, imageUrl, false);
                if (isStlEntity)
                {
                    retVal = imageUrl;
                }
                else
                {
                    if (!imageUrl.ToUpper().Trim().EndsWith(".SWF"))
                    {
                        var imageAttributes = new NameValueCollection();
                        TranslateUtils.AddAttributesIfNotExists(imageAttributes, attributes);
                        imageAttributes["src"] = imageUrl;

                        retVal = $@"<img {TranslateUtils.ToAttributesString(attributes)}>";
                    }
                    else
                    {
                        var width = 100;
                        var height = 100;
                        if (attributes != null)
                        {
                            if (!string.IsNullOrEmpty(attributes["width"]))
                            {
                                width = TranslateUtils.ToInt(attributes["width"]);
                            }
                            if (!string.IsNullOrEmpty(attributes["height"]))
                            {
                                height = TranslateUtils.ToInt(attributes["height"]);
                            }
                        }
                        retVal = $@"
<object classid=""clsid:D27CDB6E-AE6D-11cf-96B8-444553540000"" codebase=""http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,29,0"" width=""{width}"" height=""{height}"">
                <param name=""movie"" value=""{imageUrl}"">
                <param name=""quality"" value=""high"">
                <param name=""wmode"" value=""transparent"">
                <embed src=""{imageUrl}"" width=""{width}"" height=""{height}"" quality=""high"" pluginspage=""http://www.macromedia.com/go/getflashplayer"" type=""application/x-shockwave-flash"" wmode=""transparent""></embed></object>
";
                    }
                }
            }
            return retVal;
        }

        public async Task<string> GetVideoHtmlAsync(Config config, Site site, string videoUrl, NameValueCollection attributes, bool isStlEntity)
        {
            var retVal = string.Empty;
            if (!string.IsNullOrEmpty(videoUrl))
            {
                videoUrl = await _pathManager.ParseNavigationUrlAsync(site, videoUrl, false);
                if (isStlEntity)
                {
                    retVal = videoUrl;
                }
                else
                {
                    retVal = $@"
<embed src=""{SiteFilesAssets.GetUrl(_pathManager.GetApiUrl(config), SiteFilesAssets.BrPlayer.Swf)}"" allowfullscreen=""true"" flashvars=""controlbar=over&autostart={true
                        .ToString().ToLower()}&image={string.Empty}&file={videoUrl}"" width=""{450}"" height=""{350}""/>
";
                }
            }
            return retVal;
        }

        public string GetFileHtmlWithCount(Config config, Site site, int channelId, int contentId, string fileUrl, NameValueCollection attributes, string innerHtml, bool isStlEntity, bool isLower, bool isUpper)
        {
            if (site == null || string.IsNullOrEmpty(fileUrl)) return string.Empty;

            string retVal;
            if (isStlEntity)
            {
                retVal = _pathManager.GetDownloadApiUrl(_pathManager.GetApiUrl(config), site.Id, channelId, contentId,
                    fileUrl);
            }
            else
            {
                var linkAttributes = new NameValueCollection();
                TranslateUtils.AddAttributesIfNotExists(linkAttributes, attributes);
                linkAttributes["href"] = _pathManager.GetDownloadApiUrl(_pathManager.GetApiUrl(config), site.Id, channelId,
                    contentId, fileUrl);

                innerHtml = string.IsNullOrEmpty(innerHtml)
                    ? PageUtils.GetFileNameFromUrl(fileUrl)
                    : innerHtml;

                if (isLower)
                {
                    innerHtml = innerHtml.ToLower();
                }
                if (isUpper)
                {
                    innerHtml = innerHtml.ToUpper();
                }

                retVal = $@"<a {TranslateUtils.ToAttributesString(linkAttributes)}>{innerHtml}</a>";
            }

            return retVal;
        }

        public string GetFileHtmlWithoutCount(Config config, Site site, string fileUrl, NameValueCollection attributes, string innerHtml, bool isStlEntity, bool isLower, bool isUpper)
        {
            if (site == null || string.IsNullOrEmpty(fileUrl)) return string.Empty;

            string retVal;
            if (isStlEntity)
            {
                retVal = _pathManager.GetDownloadApiUrl(_pathManager.GetApiUrl(config), site.Id, fileUrl);
            }
            else
            {
                var linkAttributes = new NameValueCollection();
                TranslateUtils.AddAttributesIfNotExists(linkAttributes, attributes);
                linkAttributes["href"] = _pathManager.GetDownloadApiUrl(_pathManager.GetApiUrl(config), site.Id, fileUrl);
                innerHtml = string.IsNullOrEmpty(innerHtml) ? PageUtils.GetFileNameFromUrl(fileUrl) : innerHtml;

                if (isLower)
                {
                    innerHtml = innerHtml.ToLower();
                }
                if (isUpper)
                {
                    innerHtml = innerHtml.ToUpper();
                }

                retVal = $@"<a {TranslateUtils.ToAttributesString(linkAttributes)}>{innerHtml}</a>";
            }

            return retVal;
        }
    }
}
