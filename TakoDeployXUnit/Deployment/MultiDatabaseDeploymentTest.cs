﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TakoDeployCore;
using TakoDeploy.Core.Data.Context;
using TakoDeploy.Core.Model;
using TakoDeployXUnit.Fixtures;
using Xunit;

namespace TakoDeployXUnit.Deployment
{
    [Collection("Database collection")]
    public class MultiDatabaseDeploymentTest
    {
        DatabaseFixture DBF;
        public MultiDatabaseDeploymentTest(DatabaseFixture database)
        {
            DBF = database;
            DocumentManager.Current = new DocumentManager();
        }

        [Fact]
        public async Task Deploy()
        {
            DocumentManager.Current.DeploymentEvent += (a, b) => 
            {
                if (b.Exception != null)
                {
                    throw b.Exception;
                }
            };
            ClearAndAddSource();
            CleanAndAddNorthwind();
            
            //if (DocumentManager.Current.Deployment.Status != DeploymentStatus.Idle)
            //    throw new Exception("Fix this test");
            await DocumentManager.Current.Deploy();
            //if(DocumentManager.Current.Deployment.Status == DeploymentStatus.Error)
            //    throw new Exception("Fix this test");
            foreach (var item in DocumentManager.Current.Deployment.Targets)
            {
                var count = GetTableCount(item);
                if (count < 11)
                    Assert.True(false);
            }
            Assert.True(true);
        }

        [Fact]
        public async Task CommandTimeout_Pass()
        {
            PrepareForTimeout_4SecondExecutionQuery(5);

            await DocumentManager.Current.Deploy();

            Assert.Equal(Database.DatabaseDeploymentState.Success,
                DocumentManager.Current.Deployment.Targets.First().DeploymentState);
        }

        [Fact]
        public async Task CommandTimeout_Fail()
        {
            PrepareForTimeout_4SecondExecutionQuery(3);


            await DocumentManager.Current.Deploy();

            Assert.Equal(Database.DatabaseDeploymentState.Error,
                DocumentManager.Current.Deployment.Targets.First().DeploymentState);

        }

        private void PrepareForTimeout_4SecondExecutionQuery(int timeoutSeconds)
        {
            DocumentManager.Current.Deployment.ScriptFiles.Clear();
            DocumentManager.Current.Deployment.ScriptFiles.Add(
                new TakoDeploy.Core.Scripts.ScriptFile(DocumentManager.Current.GetParser())
                {
                    Content = @"
                            PRINT 'Waiting'
                            WAITFOR DELAY '00:00:4';
                            PRINT 'ok'
                    "
                });
            DocumentManager.Current.Deployment.Sources.Clear();
            DocumentManager.Current.Deployment.Sources.Add(
                new TakoDeploy.Core.Model.SourceDatabase()
                {
                    ConnectionString = DBF.ConnectionString,
                    ProviderType = DBF.ProviderType,
                    Type = SourceType.Direct,
                    CommandTimeout = timeoutSeconds
                }
            );
        }

        private int? GetTableCount(TargetDatabase target)
        {
            var context = new TakoConnectionFactory().GetDbContext(target.ProviderType, target.ConnectionString);
            int? result = null;
            //using (var connection = context.)
            {
                //connection.ConnectionString = target.ConnectionString;
                //connection.Open();
                //var command = connection.CreateCommand();
                //command.CommandText = "SELECT COUNT(1) as [TablesCount] FROM Sys.Tables";
                //result = command.ExecuteScalar() as int?;
                //TODO: finish this
            }
            return result;
        }
        
        private void CleanAndAddNorthwind()
        {
            DocumentManager.Current.Deployment.ScriptFiles.Clear();
            DocumentManager.Current.Deployment.ScriptFiles.Add(
                new TakoDeploy.Core.Scripts.ScriptFile(DocumentManager.Current.GetParser()) {
                    Content = ScriptManager.GetNorthwind()
                });
        }

        private void ClearAndAddSource()
        {
            DocumentManager.Current.Deployment.Sources.Clear();
            DocumentManager.Current.Deployment.Sources.Add(
                new TakoDeploy.Core.Model.SourceDatabase()
                {
                    ConnectionString = DBF.ConnectionString,
                    ProviderType = DBF.ProviderType,
                    Type = SourceType.DataSource,
                    NameFilter = "Tako"
                }
            );
        }
    }
}
