using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;
using System.IO;
using System.Configuration;
using System.Globalization; 

namespace serverTcp.Database
{
    class SQLiteDatabase
    {
        protected String dbConnection;

        /// <summary>  
        ///     Single Param Constructor for specifying the DB file.  
        /// </summary>  
        /// <param name="inputFile">The File containing the DB</param>  
        public SQLiteDatabase(String inputFile)
        {
            string sourceFile = inputFile;
            dbConnection = String.Format("Data Source={0}", sourceFile);
        }


        /// <summary>  
        ///     Allows the programmer to interact with the database for purposes other than a query.  
        /// </summary>  
        /// <param name="sql">The SQL to be run.</param>  
        /// <returns>An Integer containing the number of rows updated.</returns>  
        public int ExecuteNonQuery(string sql)  
        {  
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            SQLiteTransaction t = null;
            int rowsUpdated = -1;
            cnn.Open();
            try
            {
                t = cnn.BeginTransaction();
                SQLiteCommand mycommand = new SQLiteCommand(cnn);
                mycommand.Transaction = t;
                mycommand.CommandText = sql;
                rowsUpdated = mycommand.ExecuteNonQuery();
                t.Commit();
                
            }catch (SQLiteException)
            {
                if (t != null)
                {
                    try
                    {
                        t.Rollback();
                    }
                    catch (SQLiteException ex2)
                    {
                        Console.WriteLine("Transaction rollback failed");
                        Console.WriteLine("Error: {0}", ex2.ToString());
                    }
                    finally
                    {
                        t.Dispose();
                    }
                }
            }
            if (cnn != null)
                cnn.Close();  
            return rowsUpdated;  
        }

        /// <summary>  
        ///     Allows the programmer to run a query against the Database.  
        /// </summary>  
        /// <param name="sql">The SQL to run</param>  
        /// <returns>A DataTable containing the result set.</returns>  
        public DataTable GetDataTable(string sql)
        {
            DataTable dt = new DataTable();
            try
            {
                SQLiteConnection cnn = new SQLiteConnection(dbConnection);
                cnn.Open();
                SQLiteCommand mycommand = new SQLiteCommand(cnn);
                mycommand.CommandText = sql;
                SQLiteDataReader reader = mycommand.ExecuteReader();
                dt.Load(reader);
                reader.Close();
                cnn.Close();
            }
            catch (Exception ex)
            {
            }
            return dt;
        }  
  
        /// <summary>  
        ///     Allows the programmer to retrieve single items from the DB.  
        /// </summary>  
        /// <param name="sql">The query to run.</param>  
        /// <returns>A string.</returns>  
        public string ExecuteScalar(string sql)  
        {
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            SQLiteTransaction t = null;
            Object value = null;
            cnn.Open();
            try
            {
                t = cnn.BeginTransaction();
                SQLiteCommand mycommand = new SQLiteCommand(cnn);
                mycommand.Transaction = t;
                mycommand.CommandText = sql;
                value = mycommand.ExecuteScalar();
                t.Commit();

            }
            catch (SQLiteException)
            {
                if (t != null)
                {
                    try
                    {
                        t.Rollback();
                    }
                    catch (SQLiteException ex2)
                    {
                        Console.WriteLine("Transaction rollback failed");
                        Console.WriteLine("Error: {0}", ex2.ToString());
                    }
                    finally
                    {
                        t.Dispose();
                    }
                }
            }
            if (cnn != null) cnn.Close();
            if (value != null) return value.ToString();  
   
            return "";  
        }



        /// <summary>  
        ///     Allows the programmer to retrieve single items from the DB.  
        /// </summary>  
        /// <param name="sql">The query to run.</param>  
        /// <returns>A string.</returns>  
        public String ExecuteSelectMultiRow(String sql, String key, String value, String time=null)
        {
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            SQLiteTransaction t = null;
            String values = "";
            cnn.Open();
            try
            {
                t = cnn.BeginTransaction();
                SQLiteCommand mycommand = new SQLiteCommand(cnn);
                mycommand.Transaction = t;
                mycommand.CommandText = sql;
                SQLiteDataReader r = mycommand.ExecuteReader();
                while (r.Read())
                {
                    if (time != null)
                        values += Convert.ToString(r[key]) + "%" + Convert.ToString(r[value]) + "%" + Convert.ToString(r[time]) + ";";
                    else
                        value += Convert.ToString(r[key]) + "\\" + Convert.ToString(r[value]);
                }
                t.Commit();

            }
            catch (SQLiteException)
            {
                if (t != null)
                {
                    try
                    {
                        t.Rollback();
                    }
                    catch (SQLiteException ex2)
                    {
                        Console.WriteLine("Transaction rollback failed");
                        Console.WriteLine("Error: {0}", ex2.ToString());
                    }
                    finally
                    {
                        t.Dispose();
                    }
                }
            }
            if (cnn != null) cnn.Close();
            if (values != null) return values;

            return "";
        }  
  
        /// <summary>  
        ///     Allows the programmer to easily update rows in the DB.  
        /// </summary>  
        /// <param name="tableName">The table to update.</param>  
        /// <param name="data">A dictionary containing Column names and their new values.</param>  
        /// <param name="where">The where clause for the update statement.</param>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public bool Update(String tableName, Dictionary<String, String> data, String where)  
        {  
            String vals = "";  
            Boolean returnCode = true;  
            if (data.Count >= 1)  
            {  
                foreach (KeyValuePair<String, String> val in data)  
                {  
                    vals += String.Format(" {0} = '{1}',", val.Key.ToString(), val.Value.ToString());  
                }  
                vals = vals.Substring(0, vals.Length - 1);  
            }  
            try  
            {  
                this.ExecuteNonQuery(String.Format("update {0} set {1} where {2};", tableName, 
                                       vals, where));  
            }  
            catch(Exception ex)  
            {                  
                returnCode = false;  
                //ServiceLogWriter.LogError(ex);  
            }  
            return returnCode;  
        }  
  
        /// <summary>  
        ///     Allows the programmer to easily delete rows from the DB.  
        /// </summary>  
        /// <param name="tableName">The table from which to delete.</param>  
        /// <param name="where">The where clause for the delete.</param>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public bool Delete(String tableName, String where)  
        {  
            Boolean returnCode = true;  
            try  
            {  
                this.ExecuteNonQuery(String.Format("delete from {0} where {1};", tableName, where));  
            }  
            catch (Exception ex)  
            {  
                returnCode = false;  
            }  
            return returnCode;  
        }  
  
        /// <summary>  
        ///     Allows the programmer to easily insert into the DB  
        /// </summary>  
        /// <param name="tableName">The table into which we insert the data.</param>  
        /// <param name="data">A dictionary containing the column names and data for the insert.</param>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public bool Insert(String tableName, Dictionary<String, String> data)  
        {  
            String columns = "";  
            String values = "";  
            Boolean returnCode = true;  
            foreach (KeyValuePair<String, String> val in data)  
            {  
                columns += String.Format(" {0},", val.Key.ToString());  
                values += String.Format(" '{0}',", val.Value);  
            }  
            columns = columns.Substring(0, columns.Length - 1);  
            values = values.Substring(0, values.Length - 1);  
            try  
            {  
                this.ExecuteNonQuery(String.Format("insert into {0}({1}) values({2});", tableName, columns, values));  
            }  
            catch (Exception ex)  
            {  
                returnCode = false;  
            }  
            return returnCode;  
        }  
  
        /// <summary>  
        ///     Allows the programmer to easily delete all data from the DB.  
        /// </summary>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public bool ClearDB()  
        {  
            DataTable tables;  
            try  
            {  
                tables = this.GetDataTable("select NAME from SQLITE_MASTER where type='table' order by NAME;");  
                foreach (DataRow table in tables.Rows)  
                {  
                    this.ClearTable(table["NAME"].ToString());  
                }  
                return true;  
            }  
            catch  
            {  
                return false;  
            }  
        }  
  
        /// <summary>  
        ///     Allows the user to easily clear all data from a specific table.  
        /// </summary>  
        /// <param name="table">The name of the table to clear.</param>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public bool ClearTable(String table)  
        {  
            try  
            {  
  
                this.ExecuteNonQuery(String.Format("delete from {0};", table));  
                return true;  
            }  
            catch  
            {  
                return false;  
            }  
        }  
  
        /// <summary>  
        ///     Allows the programmer to easily test connect to the DB.  
        /// </summary>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public bool TestConnection()  
        {  
            using (SQLiteConnection cnn = new SQLiteConnection(dbConnection))  
            {  
                try  
                {  
                    cnn.Open();  
                    return true;  
                }  
                catch  
                {  
                    return false;  
                }  
                finally  
                {  
                    // Close the database connection  
                    if ((cnn != null) && (cnn.State != ConnectionState.Open))  
                        cnn.Close();  
                }  
            }  
        }  
  
        /// <summary>  
        ///     Allows the programmer to easily test if table exists in the DB.  
        /// </summary>  
        /// <returns>A boolean true or false to signify success or failure.</returns>  
        public bool IsTableExists(String tableName)  
        {  
            string count = "0";  
            if (dbConnection == default(string))  
                return false;  
            using (SQLiteConnection cnn = new SQLiteConnection(dbConnection))  
            {  
                try  
                {  
                    cnn.Open();  
                    if (tableName == null || cnn.State != ConnectionState.Open)  
                    {  
                        return false;  
                    }  
                    String sql = string.Format("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name ='{0}'", tableName);  
                    count = ExecuteScalar(sql);  
                }  
                finally  
                {  
                    // Close the database connection  
                    if ((cnn != null) && (cnn.State != ConnectionState.Open))  
                        cnn.Close();  
                }  
            }  
            return Convert.ToInt32(count) > 0;  
        }  
    }
}
