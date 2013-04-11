﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;

/// <summary>
/// Descripción breve de LcLogger
/// </summary>
public class LcLogger : IDisposable
{
    StringBuilder logger;
    string logName;
    /// <summary>
    /// Create a new logger.
    /// </summary>
    /// <param name="logName">Name of the log, will be the prefix name of the file</param>
    public LcLogger(string logName)
    {
        logger = new StringBuilder();
        this.logName = logName;
    }
    /// <summary>
    /// Log a text line.
    /// Every line logged include a date-time mark in the start and a new-line character at the end automatically.
    /// Multiline is not allowed in the text, any new-line character is stripped (replaced by a double white space),
    /// to log multi-line preserving original format, use LogData, but remember call previously to Log with a single-line title
    /// to document date-time and subject of the long text/data.
    /// </summary>
    /// <param name="format"></param>
    /// <param name="pars"></param>
    public void Log(string format, params object[] pars){
        // Universal date-time, following ISO8601 format with Z identifier at the end
        logger.AppendFormat("{0:s}Z ", DateTime.Now.ToUniversalTime());
        logger.AppendFormat(format.Replace("\n", "  "), pars);
        logger.Append("\n");
    }
    /// <summary>
    /// Log an Exception with detailed information and inner exception if exists.
    /// </summary>
    /// <param name="task"></param>
    /// <param name="ex"></param>
    public void LogEx(string task, Exception ex){
        Log("{0}: Exception {1}", task, ex.Message);
        LogData("{0} {1}", innerExToString(ex), ex.StackTrace);
    }
    /// <summary>
    /// Log a multiline text.
    /// Remember add a call to Log method before of this with a single-line title
    /// to document date-time and subject of the long text/data added with this method.
    /// </summary>
    /// <param name="format"></param>
    /// <param name="pars"></param>
    public void LogData(string format, params object[] pars)
    {
        logger.AppendFormat("[LOGDATA[\n {0} \n]LOGDATA]\n", String.Format(format, pars));
    }
    string innerExToString(Exception ex){
        if (ex.InnerException != null) {
            return String.Format("InnerException {0}\n{1}", ex.InnerException, innerExToString(ex.InnerException));
        }
        return "";
    }
    public void Save()
    {
        string path = String.Format("_logs/{0}{1:yyyyMM}.log.txt", logName, DateTime.Today);
        if (HttpContext.Current != null)
            path = HttpContext.Current.Server.MapPath(LcUrl.RenderAppPath + path);
        System.IO.File.AppendAllText(path, logger.ToString());
    }
    /// <summary>
    /// Returns the full text logged.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return logger.ToString();
    }
    /// <summary>
    /// Free ressources
    /// </summary>
    public void Dispose()
    {
        logger.Clear();
        logger = null;
    }
}