﻿using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Http
{
    /// <summary>Http请求响应基类</summary>
    public abstract class HttpBase
    {
        #region 属性
        /// <summary>是否WebSocket</summary>
        public Boolean IsWebSocket { get; set; }

        ///// <summary>是否启用SSL</summary>
        //public Boolean IsSSL { get; set; }

        /// <summary>内容长度</summary>
        public Int32 ContentLength { get; set; }

        /// <summary>头部集合</summary>
        public IDictionary<String, Object> Headers { get; set; } = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>获取/设置 头部</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public String this[String key] { get { return Headers[key] + ""; } set { Headers[key] = value; } }
        #endregion

        #region 解析
        /// <summary>过期时间</summary>
        internal DateTime Expire { get; set; }

        /// <summary>是否已完整</summary>
        internal Boolean IsCompleted { get { return ContentLength == 0 || ContentLength <= BodyLength; } }

        /// <summary>主体长度</summary>
        internal Int32 BodyLength { get; set; }

        internal Boolean ParseHeader(Packet pk)
        {
            var p = (Int32)pk.Data.IndexOf(pk.Offset, pk.Count, "\r\n\r\n".GetBytes());
            if (p < 0) return false;

#if DEBUG
            //WriteLog(pk.ToStr());
#endif

            // 截取
            var lines = pk.ReadBytes(0, p).ToStr().Split("\r\n");
            // 重构
            p += 4;
            pk.Set(pk.Data, pk.Offset + p, pk.Count - p);

            // 分析头部
            //headers.Clear();
            var line = lines[0];
            for (var i = 1; i < lines.Length; i++)
            {
                line = lines[i];
                p = line.IndexOf(':');
                if (p > 0) Headers[line.Substring(0, p)] = line.Substring(p + 1).Trim();
            }

            // 分析第一行
            OnParse(lines[0]);

            //// 判断主体长度
            //BodyLength += pk.Count;
            //if (ContentLength > 0 && BodyLength >= ContentLength) IsCompleted = true;

            return false;
        }

        /// <summary>分析第一行</summary>
        /// <param name="firstLine"></param>
        protected abstract void OnParse(String firstLine);

        private MemoryStream _cache;
        internal Boolean ParseBody(ref Packet pk)
        {
            if (IsWebSocket) return true;

            BodyLength += pk.Count;
            //XTrace.WriteLine("{0}/{1}/{2}", pk.Count, BodyLength, ContentLength);

            if (_cache == null) _cache = new MemoryStream();
            pk.WriteTo(_cache);

            if (!IsCompleted) return false;

            pk = _cache.ToArray();
            _cache = null;

            return true;
        }
        #endregion

        #region 读写
        /// <summary>创建请求响应包</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Packet Build(Packet data)
        {
            var len = data != null ? data.Count : 0;

            var rs = new Packet(BuildHeader(len).GetBytes());
            rs.Next = data;

            return rs;
        }

        /// <summary>创建头部</summary>
        /// <param name="length"></param>
        /// <returns></returns>
        protected abstract String BuildHeader(Int32 length);
        #endregion
    }
}