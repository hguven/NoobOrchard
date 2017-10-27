using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using Orchard.Utility;

namespace Orchard.Environment
{
    /// <summary>
    /// Request������
    /// </summary>
    public class RequestHelper
    {
        /// <summary>
        /// �жϵ�ǰҳ���Ƿ���յ���Post����
        /// </summary>
        /// <returns>�Ƿ���յ���Post����</returns>
        public static bool IsPost(HttpRequestBase request = null)
        {
            if (request == null)
            {
                request = new HttpRequestWrapper(HttpContext.Current.Request);
            }
            return request.HttpMethod.Equals("POST");
        }
        /// <summary>
        /// �жϵ�ǰҳ���Ƿ���յ���Get����
        /// </summary>
        /// <returns>�Ƿ���յ���Get����</returns>
        public static bool IsGet(HttpRequestBase request = null)
        {
            if (request == null)
            {
                request = new HttpRequestWrapper(HttpContext.Current.Request);
            }
            return request.HttpMethod.Equals("GET");
        }

        /// <summary>
        /// ����ָ���ķ�����������Ϣ
        /// </summary>
        /// <param name="strName">������������</param>
        /// <returns>������������Ϣ</returns>
        public static string GetServerString(string strName)
        {
            //
            if (HttpContext.Current.Request.ServerVariables[strName] == null)
            {
                return "";
            }
            return HttpContext.Current.Request.ServerVariables[strName].ToString();
        }

        /// <summary>
        /// ������һ��ҳ��ĵ�ַ
        /// </summary>
        /// <returns>��һ��ҳ��ĵ�ַ</returns>
        public static string GetUrlReferrer()
        {
            if (HttpContext.Current.Request.UrlReferrer == null) return string.Empty;
            string retVal = null;

            try
            {
                retVal = HttpContext.Current.Request.UrlReferrer.ToString();
            }
            catch { }

            if (retVal == null)
                return "";

            return retVal;

        }

        /// <summary>
        /// �õ���ǰ��������ͷ
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentFullHost()
        {
            HttpRequest request = System.Web.HttpContext.Current.Request;
            if (!request.Url.IsDefaultPort)
            {
                return string.Format("{0}://{1}:{2}", request.Url.Scheme, request.Url.Host, request.Url.Port.ToString());
            }
            return string.Format("{0}://{1}", request.Url.Scheme, request.Url.Host);
        }

        /// <summary>
        /// �õ�����ͷ
        /// </summary>
        /// <returns></returns>
        public static string GetHost()
        {
            return HttpContext.Current.Request.Url.Host;
        }


        /// <summary>
        /// ��ȡ��ǰ�����ԭʼ URL(URL ������Ϣ֮��Ĳ���,������ѯ�ַ���(�������))
        /// </summary>
        /// <returns>ԭʼ URL</returns>
        public static string GetRawUrl()
        {
            return HttpContext.Current.Request.RawUrl;
        }

        /// <summary>
        /// �жϵ�ǰ�����Ƿ�������������
        /// </summary>
        /// <returns>��ǰ�����Ƿ�������������</returns>
        public static bool IsBrowserGet()
        {
            string[] BrowserName = { "ie", "opera", "netscape", "mozilla", "konqueror", "firefox" };
            string curBrowser = HttpContext.Current.Request.Browser.Type.ToLower();
            for (int i = 0; i < BrowserName.Length; i++)
            {
                if (curBrowser.IndexOf(BrowserName[i]) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// �ж��Ƿ�����������������
        /// </summary>
        /// <returns>�Ƿ�����������������</returns>
        public static bool IsSearchEnginesGet()
        {
            if (HttpContext.Current.Request.UrlReferrer == null && HttpContext.Current.Request.UserAgent == null)
            {
                return false;
            }
            string[] SearchEngine = { "google", "yahoo", "msn", "baidu", "sogou", "sohu", "sina", "163", "lycos", "tom", "yisou", "iask", "soso", "gougou", "zhongsou" };
            if (HttpContext.Current.Request.UrlReferrer != null)
            {
                string tmpReferrer = HttpContext.Current.Request.UrlReferrer.ToString().ToLower();
                tmpReferrer = Regex.Replace(tmpReferrer, "url=([\\s\\S])+", string.Empty);
                for (int i = 0; i < SearchEngine.Length; i++)
                {
                    if (tmpReferrer.IndexOf(SearchEngine[i]) >= 0)
                    {
                        return true;
                    }
                }
            }
            else
            {
                string strUserAgent = HttpContext.Current.Request.UserAgent;
                for (int i = 0; i < SearchEngine.Length; i++)
                {
                    if (strUserAgent.IndexOf(SearchEngine[i]) >= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// ��õ�ǰ����Url��ַ
        /// </summary>
        /// <returns>��ǰ����Url��ַ</returns>
        public static string GetUrl()
        {
            return HttpContext.Current.Request.Url.ToString();
        }


        /// <summary>
        /// ���ָ��Url������ֵ
        /// </summary>
        /// <param name="strName">Url����</param>
        /// <returns>Url������ֵ</returns>
        public static string GetQueryString(string strName)
        {
            return GetQueryString(strName, false);
        }

        /// <summary>
        /// ���ָ��Url������ֵ
        /// </summary> 
        /// <param name="strName">Url����</param>
        /// <param name="sqlSafeCheck">�Ƿ����SQL��ȫ���</param>
        /// <returns>Url������ֵ</returns>
        public static string GetQueryString(string strName, bool sqlSafeCheck)
        {
            if (HttpContext.Current.Request.QueryString[strName] == null)
            {
                return "";
            }

            if (sqlSafeCheck && !Utils.IsSafeSqlString(HttpContext.Current.Request.QueryString[strName]))
            {
                return "unsafe string";
            }

            return HttpContext.Current.Request.QueryString[strName];
        }

        /// <summary>
        /// ��õ�ǰҳ�������
        /// </summary>
        /// <returns>��ǰҳ�������</returns>
        public static string GetPageName()
        {
            string[] urlArr = HttpContext.Current.Request.Url.AbsolutePath.Split('/');
            return urlArr[urlArr.Length - 1].ToLower();
        }

        /// <summary>
        /// ���ر���Url�������ܸ���
        /// </summary>
        /// <returns></returns>
        public static int GetParamCount()
        {
            return HttpContext.Current.Request.Form.Count + HttpContext.Current.Request.QueryString.Count;
        }


        /// <summary>
        /// ���ָ����������ֵ
        /// </summary>
        /// <param name="strName">������</param>
        /// <returns>��������ֵ</returns>
        public static string GetFormString(string strName)
        {
            return GetFormString(strName, false);
        }

        /// <summary>
        /// ���ָ����������ֵ
        /// </summary>
        /// <param name="strName">������</param>
        /// <param name="sqlSafeCheck">�Ƿ����SQL��ȫ���</param>
        /// <returns>��������ֵ</returns>
        public static string GetFormString(string strName, bool sqlSafeCheck)
        {
            if (HttpContext.Current.Request.Form[strName] == null)
            {
                return "";
            }

            if (sqlSafeCheck && !Utils.IsSafeSqlString(HttpContext.Current.Request.Form[strName]))
            {
                return "unsafe string";
            }

            return HttpContext.Current.Request.Form[strName];
        }

        /// <summary>
        /// ���Url���������ֵ, ���ж�Url�����Ƿ�Ϊ���ַ���, ��ΪTrue�򷵻ر�������ֵ
        /// </summary>
        /// <param name="strName">����</param>
        /// <returns>Url���������ֵ</returns>
        public static string GetString(string strName)
        {
            return GetString(strName, false);
        }

        /// <summary>
        /// ���Url���������ֵ, ���ж�Url�����Ƿ�Ϊ���ַ���, ��ΪTrue�򷵻ر�������ֵ
        /// </summary>
        /// <param name="strName">����</param>
        /// <param name="sqlSafeCheck">�Ƿ����SQL��ȫ���</param>
        /// <returns>Url���������ֵ</returns>
        public static string GetString(string strName, bool sqlSafeCheck)
        {
            if ("".Equals(GetQueryString(strName)))
            {
                return GetFormString(strName, sqlSafeCheck);
            }
            else
            {
                return GetQueryString(strName, sqlSafeCheck);
            }
        }


        /// <summary>
        /// ���ָ��Url������int����ֵ
        /// </summary>
        /// <param name="strName">Url����</param>
        /// <param name="defValue">ȱʡֵ</param>
        /// <returns>Url������int����ֵ</returns>
        public static int GetQueryInt(string strName, int defValue)
        {
            return HttpContext.Current.Request.QueryString[strName].ToInt(defValue);
        }


        /// <summary>
        /// ���ָ����������int����ֵ
        /// </summary>
        /// <param name="strName">������</param>
        /// <param name="defValue">ȱʡֵ</param>
        /// <returns>��������int����ֵ</returns>
        public static int GetFormInt(string strName, int defValue)
        {
            return HttpContext.Current.Request.Form[strName].ToInt(defValue);
        }

        /// <summary>
        /// ���ָ��Url���������int����ֵ, ���ж�Url�����Ƿ�Ϊȱʡֵ, ��ΪTrue�򷵻ر�������ֵ
        /// </summary>
        /// <param name="strName">Url�������</param>
        /// <returns>Url���������int����ֵ</returns>
        public static int GetInt(string strName)
        {
            return GetInt(strName, 0);
        }
        /// <summary>
        /// ���ָ��Url���������int����ֵ, ���ж�Url�����Ƿ�Ϊȱʡֵ, ��ΪTrue�򷵻ر�������ֵ
        /// </summary>
        /// <param name="strName">Url�������</param>
        /// <param name="defValue">ȱʡֵ</param>
        /// <returns>Url���������int����ֵ</returns>
        public static int GetInt(string strName, int defValue)
        {
            if (GetQueryInt(strName, defValue) == defValue)
            {
                return GetFormInt(strName, defValue);
            }
            else
            {
                return GetQueryInt(strName, defValue);
            }
        }

        /// <summary>
        /// ���ָ��Url������float����ֵ
        /// </summary>
        /// <param name="strName">Url����</param>
        /// <param name="defValue">ȱʡֵ</param>
        /// <returns>Url������int����ֵ</returns>
        public static float GetQueryFloat(string strName, float defValue)
        {
            return HttpContext.Current.Request.QueryString[strName].ToFloat(defValue);
        }


        /// <summary>
        /// ���ָ����������float����ֵ
        /// </summary>
        /// <param name="strName">������</param>
        /// <param name="defValue">ȱʡֵ</param>
        /// <returns>��������float����ֵ</returns>
        public static float GetFormFloat(string strName, float defValue)
        {
            return HttpContext.Current.Request.Form[strName].ToFloat(defValue);
        }

        /// <summary>
        /// ���ָ��Url���������float����ֵ, ���ж�Url�����Ƿ�Ϊȱʡֵ, ��ΪTrue�򷵻ر�������ֵ
        /// </summary>
        /// <param name="strName">Url�������</param>
        /// <param name="defValue">ȱʡֵ</param>
        /// <returns>Url���������int����ֵ</returns>
        public static float GetFloat(string strName, float defValue)
        {
            if (GetQueryFloat(strName, defValue) == defValue)
            {
                return GetFormFloat(strName, defValue);
            }
            else
            {
                return GetQueryFloat(strName, defValue);
            }
        }

        /// <summary>
        /// ��õ�ǰҳ��ͻ��˵�IP
        /// </summary>
        /// <returns>��ǰҳ��ͻ��˵�IP</returns>
        public static string GetIP()
        {
            string result = String.Empty;
            HttpContext context = HttpContext.Current;
            if (context != null&&context.Request!=null)
            {
                result = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(result))
                {
                    result = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = HttpContext.Current.Request.UserHostAddress;
                }
            }
            if (string.IsNullOrEmpty(result) || !Utils.IsIP(result))
            {
                return "127.0.0.1";
            }
            return result;

        }

        /// <summary>
        /// �����û��ϴ����ļ�
        /// </summary>
        /// <param name="path">����·��</param>
        public static void SaveRequestFile(string path)
        {
            if (HttpContext.Current.Request.Files.Count > 0)
            {
                HttpContext.Current.Request.Files[0].SaveAs(path);
            }
        }

        /// <summary>
        /// ���ص�ǰҳ���Ƿ��ǿ�վ�ύ
        /// </summary>
        /// <returns>��ǰҳ���Ƿ��ǿ�վ�ύ</returns>
        public static bool IsCrossSitePost()
        {
            // ��������ύ��Ϊtrue
            if (!IsPost())
                return true;

            return IsCrossSitePost(GetUrlReferrer(), GetHost());
        }

        /// <summary>
        /// �ж��Ƿ��ǿ�վ�ύ
        /// </summary>
        /// <param name="urlReferrer">�ϸ�ҳ���ַ</param>
        /// <param name="host">��̳url</param>
        /// <returns>bool</returns>
        public static bool IsCrossSitePost(string urlReferrer, string host)
        {
            if (urlReferrer.Length < 7)
                return true;
            Uri u = new Uri(urlReferrer);
            return u.Host != host;
        }

        /// <summary>
        /// ��ȡ��ǰӦ����ַ
        /// </summary>
        /// <returns></returns>
        public static String GetApplicationUrl()
        {
            if (HttpContext.Current == null || HttpContext.Current.Request == null) return string.Empty;
            String url = HttpContext.Current.Request.Url.IsDefaultPort
                ? HttpContext.Current.Request.Url.Host
                : string.Format("{0}:{1}", HttpContext.Current.Request.Url.Host, HttpContext.Current.Request.Url.Port.ToString());
            if (HttpContext.Current.Request.ApplicationPath != "/")
                return "http://" + url + HttpContext.Current.Request.ApplicationPath;
            else return "http://" + url;
        }
        /// <summary>
        /// ��ȡ��ǰӦ������·��
        /// </summary>
        public static string GetApplicationPath()
        {
            try
            {
                if (HttpContext.Current == null || HttpContext.Current.Request == null) return string.Empty;
                return HttpContext.Current.Request.ApplicationPath;
            }
            catch
            {

                return string.Empty;
            }


        }
        #region  htpp����

        /// <summary>
        ///  http���󷽷�
        /// </summary>
        public enum EnumRequestMethod
        {
            /// <summary>
            /// 
            /// </summary>
            Get,
            /// <summary>
            /// 
            /// </summary>
            Post
        }
        /// <summary>
        /// http��������
        /// </summary>
        public enum EnumContentType
        {
            /// <summary>
            /// 
            /// </summary>
            Form,
            /// <summary>
            /// 
            /// </summary>
            Json,
            /// <summary>
            /// 
            /// </summary>
            Html,
            /// <summary>
            /// 
            /// </summary>
            Xml

        }

        /// <summary>
        /// ����Url��ַ����
        /// </summary>
        /// <param name="dicParms"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string CreateLinkString(IDictionary<string, string> dicParms, Encoding encoding = null)
        {
            if (dicParms == null || dicParms.Count == 0) return string.Empty;
            List<string> pairs = new List<string>();
            if (encoding == null)
            {
                encoding = Encoding.GetEncoding("utf-8");
            }
            foreach (KeyValuePair<string, string> item in dicParms)
            {
                if (string.IsNullOrEmpty(item.Value))
                    continue;
                pairs.Add(string.Format("{0}={1}", item.Key, item.Value.Replace("+", "%2B")));//Base64�ַ���
            }
            return string.Join("&", pairs.ToArray());
        }

        /// <summary>
        /// ƴ��NameValueCollection�������
        /// </summary>
        /// <param name="requestParms">�������</param>
        /// <returns></returns>
        public static string JoinRequestParmByNameValues(System.Collections.Specialized.NameValueCollection requestParms)
        {
            if (requestParms == null || requestParms.Keys.Count == 0) return string.Empty;
            List<string> pairs = new List<string>();
            Encoding encoding = Encoding.GetEncoding("utf-8");
            foreach (string item in requestParms.Keys)
            {
                pairs.Add(string.Format("{0}={1}", item, requestParms[item]));
            }
            return string.Join("&", pairs.ToArray());
        }

        /// <summary>
        /// ƴ��IDictionary�������
        /// </summary>
        /// <param name="dicParms">�������</param>
        /// <returns></returns>
        public static string JoinRequestParmByDic(IDictionary<string, string> dicParms)
        {
            if (dicParms == null || dicParms.Count == 0) return string.Empty;
            List<string> pairs = new List<string>();
            Encoding encoding = Encoding.GetEncoding("utf-8");
            foreach (KeyValuePair<string, string> item in dicParms)
            {
                if (string.IsNullOrEmpty(item.Value))
                    continue;
                pairs.Add(string.Format("{0}={1}", item.Key, item.Value.Replace("+", "%2B")));//Base64�ַ���
            }
            return string.Join("&", pairs.ToArray());
        }

        /// <summary>
        /// http����
        /// </summary>
        /// <param name="url">�����ַ</param>
        /// <param name="requestData">��������</param>
        /// <param name="contentType">���� Content-type HTTP ��ͷ��ֵ</param>
        /// <param name="method">����ʽ</param>
        /// <param name="timeout">��ʱʱ��</param>
        /// <param name="charset"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static string HttpRequest(string url, string requestData, EnumContentType contentType = EnumContentType.Form, EnumRequestMethod method = EnumRequestMethod.Get, int timeout = 30, string charset = "utf-8", Orchard.Logging.ILogger logger = null)
        {
            string rawUrl = string.Empty;
            UriBuilder uri = new UriBuilder(url);
            string result = string.Empty;
            switch (method)
            {
                case EnumRequestMethod.Get:
                    {
                        if (!string.IsNullOrEmpty(requestData))
                        {
                            uri.Query = requestData;
                        }
                    }
                    break;
            }
            HttpWebRequest http = WebRequest.Create(uri.Uri) as HttpWebRequest;
            http.ServicePoint.Expect100Continue = false;
            http.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0)";
            http.Timeout = timeout * 1000;
            // http.Referer = "";
            if (string.IsNullOrEmpty(charset)) charset = "utf-8";
            Encoding encoding = Encoding.GetEncoding(charset);
            switch (method)
            {
                case EnumRequestMethod.Get:
                    {
                        http.Method = "GET";
                    }
                    break;
                case EnumRequestMethod.Post:
                    {
                        http.Method = "POST";
                        switch (contentType)
                        {
                            case EnumContentType.Form:
                                http.ContentType = "application/x-www-form-urlencoded;charset=" + charset;
                                break;
                            case EnumContentType.Json:
                                http.ContentType = "application/json";
                                break;
                            case EnumContentType.Xml:
                                http.ContentType = "text/xml";
                                break;
                            default:
                                http.ContentType = "application/x-www-form-urlencoded;charset=" + charset;
                                break;
                        }

                        byte[] bytesRequestData = encoding.GetBytes(requestData);
                        http.ContentLength = bytesRequestData.Length;
                        using (Stream requestStream = http.GetRequestStream())
                        {
                            requestStream.Write(bytesRequestData, 0, bytesRequestData.Length);
                        }
                    }
                    break;
            }
            using (WebResponse webResponse = http.GetResponse())
            {
                using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
                {
                    result = reader.ReadToEnd();
                    reader.Close();
                }
                webResponse.Close();
            }
            http = null;
            if (logger != null)
            {
                logger.Debug("url:" + url + "\n,request:" + requestData + "\n,result:" + result);
            }
            return result;
        }
        #endregion

        /// <summary>
        /// ��ȡ֧����POST,Get����֪ͨ��Ϣ�����������ġ�������=����ֵ������ʽ�������
        /// </summary>
        /// <returns>request��������Ϣ��ɵ�����</returns>
        public static SortedDictionary<string, string> GetRequestDataBySorted(HttpRequestBase request)
        {
            int i = 0;
            SortedDictionary<string, string> dicRequest = new SortedDictionary<string, string>();
            NameValueCollection nameValues;
            if (RequestHelper.IsPost(request))
            {
                nameValues = request.Form;
                // Get names of all forms into a string array.
                String[] requestItem = nameValues.AllKeys;
                for (i = 0; i < requestItem.Length; i++)
                {
                    dicRequest.Add(requestItem[i], request.Form[requestItem[i]]);
                }
            }
            else
            {
                nameValues = request.QueryString;
                nameValues = request.Form;
                // Get names of all forms into a string array.
                String[] requestItem = nameValues.AllKeys;
                for (i = 0; i < requestItem.Length; i++)
                {
                    dicRequest.Add(requestItem[i], request.QueryString[requestItem[i]]);
                }
            }
            return dicRequest;
        }
    }
}
