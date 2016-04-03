﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Http;
using CB.Database.SqlServer;


namespace CB.Web.WebServices
{
    public class QueryController: ApiController
    {
        #region Fields
        private readonly string _connectionString;
        #endregion


        #region  Constructors & Destructor
        public QueryController(string connectionStringSetting)
        {
            _connectionString = GetConnectionString(connectionStringSetting);
        }

        public QueryController(): this("DefaultConnection") { }
        #endregion


        #region Methods
        [HttpPost]
        public DataResult GetData(DataRequestCollection requestCollection)
        {
            switch (requestCollection.QueryStrategy)
            {
                case QueryStrategy.Sequential:
                    return QueryDataSequentially(requestCollection);
                case QueryStrategy.Parallel:
                    return QueryDataParallely(requestCollection);
                case QueryStrategy.Transactional:
                    return QueryDataTransactionally(requestCollection);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion


        #region Implementation
        private static void AddCommandParameters(SqlCommand cmd, Dictionary<string, object> parameters)
        {
            /*var jParams = parameters as JObject;
            if (jParams == null) return;

            foreach (var pair in jParams)
            {
                cmd.Parameters.AddWithValue(CreateParameterName(pair.Key), pair.Value.Value<object>());
            }*/
            /*foreach (var parameter in parameters)
            {
                cmd.Parameters.Add(parameter);
            }*/
            if (parameters == null) return;

            foreach (var key in parameters.Keys)
            {
                cmd.Parameters.AddWithValue(key, parameters[key]);
            }
        }

        private static string CreateParameterName(string value)
        {
            if (!value.StartsWith("@")) value = "@" + value;
            return value;
        }

        private static string GetConnectionString(string connectionStringSetting)
        {
            return ConfigurationManager.ConnectionStrings[connectionStringSetting].ConnectionString;
        }

        private static DataSet[] GetData(DataRequestCollection requestCollection, SqlConnection con,
            SqlTransaction trans)
        {
            return requestCollection.Select(request => GetData(request, con, trans)).ToArray();
            /*var result = new DataSet();
            foreach (var ds in requestCollection.Select(request => GetData(request, con, trans)))
            {
                MergeDataSet(ds, result);
            }
            return result;*/
        }

        private static DataSet GetData(DataRequest request, SqlConnection con, SqlTransaction trans)
        {
            using (var cmd = new SqlCommand(request.Query, con) { CommandType = CommandType.StoredProcedure })
            {
                if (trans != null) cmd.Transaction = trans;
                AddCommandParameters(cmd, request.Parameters);
                var ds = new DataSet();
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(ds);
                    return ds;
                }
            }
        }

        /*private static void MergeDataSet(DataSet from, DataSet to)
        {
            foreach (DataTable table in from.Tables)
            {
                to.Tables.Add(table);
            }
        }*/

        private SqlConnection OpenConnection(string databaseName = null)
        {
            var csBuilder = new SqlConnectionStringBuilder(_connectionString);
            if (!string.IsNullOrEmpty(databaseName)) csBuilder.InitialCatalog = databaseName;
            var con = new SqlConnection(csBuilder.ToString());
            con.Open();
            return con;
        }

        private DataResult QueryDataParallely(DataRequestCollection requestCollection)
        {
            try
            {
                return new DataResult(requestCollection.AsParallel().Select(request =>
                {
                    using (var con = OpenConnection())
                    {
                        return GetData(request, con, null);
                    }
                }).ToArray());
                /*var datasets = requestCollection.AsParallel().Select(request =>
                {
                    using (var con = OpenConnection())
                    {
                        return GetData(requestCollection, con, null);
                    }
                });
                var result = new DataSet();
                foreach (var dataset in datasets)
                {
                    MergeDataSet(dataset, result);
                }
                return new DataResult(result);*/
            }
            catch (Exception exception)
            {
                return new DataResult(exception.Message);
            }
        }

        private DataResult QueryDataSequentially(DataRequestCollection requestCollection)
        {
            using (var con = OpenConnection())
            {
                try
                {
                    var ds = GetData(requestCollection, con, null);
                    return new DataResult(ds);
                }
                catch (Exception exception)
                {
                    return new DataResult(exception.Message);
                }
            }
        }

        private DataResult QueryDataTransactionally(DataRequestCollection requestCollection)
        {
            using (var con = OpenConnection())
            {
                using (var trans = con.BeginTransaction())
                {
                    try
                    {
                        var ds = GetData(requestCollection, con, trans);
                        trans.Commit();
                        return new DataResult(ds);
                    }
                    catch (Exception exception)
                    {
                        trans.Rollback();
                        return new DataResult(exception.Message);
                    }
                }
            }
        }
        #endregion
    }
}


// TODO: refactor GetData methods
// TODO: inhibit SQL injection