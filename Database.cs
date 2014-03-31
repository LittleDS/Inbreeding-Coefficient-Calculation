using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace EGDL
{
    /// <summary>
    /// This class is the database wrapper
    /// </summary>
    class Database
    {
        public String connectionString;

        private DbConnection conn = null;

        /// <summary>
        /// Constructur
        /// </summary>
        /// <param name="connString">The connection string</param>
        public Database(String connString)
        {
            connectionString = connString;
        }


        /// <summary>
        /// Using the connection string to open a new database connection
        /// </summary>
        public void OpenConnection()
        {
            //Open database connection
            if (connectionString != null)
                conn = new SqlConnection(connectionString);
            else
                throw new Exception("Connection string not specified!");

            conn.Open();
        }


        /// <summary>
        /// Close the database connection
        /// </summary>
        public void CloseConnection()
        {
            //Close databse connection
            if (conn == null) return;

            conn.Close();
            conn = null;
        }

        /// <summary>
        /// Execute a query that doesn't return an rows (INSERT, UPDATE, DELETE).
        /// The pre-processed sql string (containing string.Format() escapes {0}, {1], etc.) is
        /// then combined with the argument list and executed. 
        /// </summary>
        /// <param name="sql">Pre-processed sql string [see static readonly string memebers]</param>
        /// <param name="args">The query parameters</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string sql, params object[] args)
        {

            if (conn == null || conn.State == ConnectionState.Closed)
            {
                OpenConnection();
            }
            int r = 0;
            string query = "";

            try
            {
                query = ProcessArgs(sql, args);
                DbCommand cmd = new SqlCommand(query, (SqlConnection)conn);
                r = cmd.ExecuteNonQuery();
            }
            catch (DbException se)
            {
                Console.WriteLine(se.ToString());
            }
            return r;
        }

        /// <summary>
        /// Execute a SELECT-like query and store the results in a pre-initialized DataTable.  
        /// The pre-processed sql string (containing string.Format() escapes {0}, {1], etc.) is
        /// then combined with the argument list and executed. 
        /// </summary>
        /// <param name="dataTable">Allocated data table</param>
        /// <param name="sql">Pre-processed sql string [see static readonly string memebers]</param>
        /// <param name="args">The query parameters</param>
        /// <returns></returns>
        public int ExecuteQuery(out DataTable dataTable, string sql, params object[] args)
        {
            if (conn == null || conn.State == ConnectionState.Closed)
            {
                OpenConnection();
            }
            int r = 0;
            string query = "";

            dataTable = new DataTable();

            try
            {
                query = ProcessArgs(sql, args);

                DbCommand cmd = new SqlCommand(query, (SqlConnection)conn);

                DbDataAdapter sda = new SqlDataAdapter((SqlCommand)cmd);           

                r = sda.Fill(dataTable);
            }
            catch (DbException se)
            {
                Console.WriteLine(se.ToString());
            }
            return r;
        }

        /// <summary>
        /// Execute a query that returns a single scalar value (aggregate).
        /// The pre-processed sql string (containing string.Format() escapes {0}, {1], etc.) is
        /// then combined with the argument list and executed. 
        /// </summary>
        /// <param name="sql">Pre-processed sql string [see static readonly string memebers]</param>
        /// <param name="args">The query parameters</param>
        /// <returns></returns>
        public object ExecuteScalar(string sql, params object[] args)
        {
            if (conn == null || conn.State == ConnectionState.Closed)
            {
                OpenConnection();
            }
            object o = null;
            string query = "";

            try
            {
                query = ProcessArgs(sql, args);
                DbCommand cmd = new SqlCommand(query, (SqlConnection)conn);
                o = cmd.ExecuteScalar();
            }
            catch (DbException se)
            {
                Console.WriteLine(se.ToString());
            }
            return o;
        }

        /// <summary>
        /// Inserts values into a predefined query string using string.Format.
        /// Puts quotes around the argument to be passed to string.Format(sql, args) and then executed as an SQL command
        /// (except when the argument value is NULL).  
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private string ProcessArgs(string sql, object[] args)
        {
            if (args == null) return sql;

            // put single quotes around arguments that aren't NULL
            for (int i = 0; i < args.Length; i++)
            {
                object o = args[i];
                if (o != null)
                {
                    string s = o as string;
                    if (s != null)
                    {
                        s = s.Trim();
                        if ("".Equals(s))
                        {
                            // store NULL instead of an empty/blank string
                            o = null;
                        }
                        else
                        {
                            o = s;
                        }

                        // remove troublesome single quotes
                        o = s.Replace("'", "' + CHAR(39) + '");
                    }
                }

                if (o == null)
                {
                    args[i] = "NULL";
                }
                else if (o is bool)
                {
                    if ((bool)o)
                        args[i] = "1";
                    else
                        args[i] = "0";
                }
                else if (o is int || o is Int16 || o is Int64)
                    args[i] = o.ToString();
                else
                {
                    args[i] = "'" + o.ToString() + "'";
                }
            }

            return string.Format(sql, args);
        }
     
    }
}
