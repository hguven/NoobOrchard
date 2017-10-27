﻿using Autofac;
using NUnit.Framework;
using Orchard.Data.MongoDb.Tests.Entities;

using Orchard.Utility;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Orchard.Data.MongoDb.Tests.Repositories
{
    public class AdmArea_Repository_Tests : MongoDbTestsBase
    {
        public override void Register(ContainerBuilder builder)
        {

        }
        private Domain.Repositories.IRepository<AdmArea> _admAreaRepository;

        public override void Init()
        {
            base.Init();
            _admAreaRepository = _container.Resolve<Domain.Repositories.IRepository<AdmArea>>();
        }

        #region Select/Get/Query Tests

        /// <summary>
        /// Gets an entity with given primary key.
        /// </summary>
        /// <param name="id">Primary key of the entity to get</param>
        /// <returns>Entity</returns>
        //[Theory]
        [TestCase(1)]
        public void AdmArea_Single_Test(int id)
        {
            //exec sp_executesql N'SELECT AdmAreaMap0_.Id as Id1_0_0_, AdmAreaMap0_.AreaID as AreaID2_0_0_, AdmAreaMap0_.AreaName as AreaNa3_0_0_, AdmAreaMap0_.ParentId as Parent4_0_0_, AdmAreaMap0_.ShortName as ShortN5_0_0_, AdmAreaMap0_.LevelType as LevelT6_0_0_, AdmAreaMap0_.CityCode as CityCo7_0_0_, AdmAreaMap0_.ZipCode as ZipCod8_0_0_, AdmAreaMap0_.AreaNamePath as AreaNa9_0_0_, AdmAreaMap0_.AreaIDPath as AreaI10_0_0_, AdmAreaMap0_.Lng as Lng11_0_0_, AdmAreaMap0_.Lat as Lat12_0_0_, AdmAreaMap0_.PinYin as PinYi13_0_0_, AdmAreaMap0_.ShortPinYin as Short14_0_0_, AdmAreaMap0_.PYFirstLetter as PYFir15_0_0_, AdmAreaMap0_.SortOrder as SortO16_0_0_, AdmAreaMap0_.Status as Statu17_0_0_, AdmAreaMap0_.CreateTime as Creat18_0_0_, AdmAreaMap0_.CreateUser as Creat19_0_0_, AdmAreaMap0_.UpdateTime as Updat20_0_0_, AdmAreaMap0_.UpdateUser as Updat21_0_0_ FROM wt_adm_area AdmAreaMap0_ WHERE AdmAreaMap0_.Id=@p0',N'@p0 int',@p0=1
            var admArea = CreateAdmArea();
            _admAreaRepository.Insert(admArea);
            id = admArea.Id;
            var AdmArea = _admAreaRepository.Single(admArea.Id);
            Assert.AreEqual(AdmArea.Id, id);
            // Console.WriteLine(JsonSerializationHelper.SerializeByDefault(AdmArea));
        }

        /// <summary>
        /// Gets an entity with given primary key.
        /// </summary>
        /// <param name="id">Primary key of the entity to get</param>
        /// <returns>Entity</returns>
        //[Theory]
        [TestCase(2)]
        public async Task AdmArea_SingleAsync_Test(int id)
        {
            var AdmArea = await _admAreaRepository.SingleAsync(id);
            Assert.AreEqual(AdmArea.Id, id);
        }

        /// <summary>
        /// Gets exactly one entity with given predicate.
        /// </summary>
        /// <param name="predicate">Entity</param>
        [TestCase(3)]
        public void AdmArea_Single_Predicate_Test(int id)
        {
            //exec sp_executesql N'select AdmAreaMap0_.Id as Id1_0_, AdmAreaMap0_.AreaID as AreaID2_0_, AdmAreaMap0_.AreaName as AreaNa3_0_, AdmAreaMap0_.ParentId as Parent4_0_, AdmAreaMap0_.ShortName as ShortN5_0_, AdmAreaMap0_.LevelType as LevelT6_0_, AdmAreaMap0_.CityCode as CityCo7_0_, AdmAreaMap0_.ZipCode as ZipCod8_0_, AdmAreaMap0_.AreaNamePath as AreaNa9_0_, AdmAreaMap0_.AreaIDPath as AreaI10_0_, AdmAreaMap0_.Lng as Lng11_0_, AdmAreaMap0_.Lat as Lat12_0_, AdmAreaMap0_.PinYin as PinYi13_0_, AdmAreaMap0_.ShortPinYin as Short14_0_, AdmAreaMap0_.PYFirstLetter as PYFir15_0_, AdmAreaMap0_.SortOrder as SortO16_0_, AdmAreaMap0_.Status as Statu17_0_, AdmAreaMap0_.CreateTime as Creat18_0_, AdmAreaMap0_.CreateUser as Creat19_0_, AdmAreaMap0_.UpdateTime as Updat20_0_, AdmAreaMap0_.UpdateUser as Updat21_0_ from wt_adm_area AdmAreaMap0_ where AdmAreaMap0_.Id=@p0',N'@p0 int',@p0=3
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            Expression<Func<AdmArea, bool>> predicate = x => x.Id == entity.Id;
            var AdmArea = _admAreaRepository.Single(predicate);
            Assert.AreEqual(AdmArea.Id, id);
        }

        /// <summary>
        /// Gets exactly one entity with given predicate.
        /// </summary>
        /// <param name="predicate">Entity</param>
        [TestCase(3)]
        public async Task AdmArea_SingleAsync_Predicate_Test(int id)
        {
            Expression<Func<AdmArea, bool>> predicate = x => x.Id == id;
            var AdmArea = await _admAreaRepository.SingleAsync(predicate);
            Assert.AreEqual(AdmArea.Id, id);
        }

        /// <summary>
        /// Creates an entity with given primary key without database access.
        /// </summary>
        /// <param name="id">Primary key of the entity to load</param>
        /// <returns>Entity</returns>
        [TestCase(4)]
        public void AdmArea_Load_Test(int id)
        {
            //exec sp_executesql N'SELECT "ID", "AreaID", "AreaName", "ParentId", "ShortName", "LevelType", "CityCode", "ZipCode", "AreaNamePath", "AreaIDPath", "lng", "Lat", "PinYin", "ShortPinYin", "PYFirstLetter", "SortOrder", "Status", "CreateTime", "CreateUser", "UpdateTime", "UpdateUser" FROM "wt_adm_area" WHERE "ID" = @ID',N'@ID int',@ID=4
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            AdmArea admArea = _admAreaRepository.Load(insertResult.Id);
            Assert.AreEqual(admArea.Id, id);
            Console.WriteLine(admArea.AreaID);
            //Console.WriteLine(JsonSerializationHelper.SerializeByDefault(AdmArea));
        }
        /// <summary>
        /// Used to get a IQueryable that is used to retrieve entities from entire table.
        /// </summary>
        /// <returns>IQueryable to be used to select entities from database</returns>
        public void AdmArea_GetAll_Test()
        {
            var AdmAreaMapAll = _admAreaRepository.GetAll();
            Assert.NotNull(AdmAreaMapAll);
        }

        /// <summary>
        /// Used to get a IQueryable that is used to retrieve entities from entire table.
        /// One or more 
        /// </summary>
        /// <param name="propertySelectors">A list of include expressions.</param>
        /// <returns>IQueryable to be used to select entities from database</returns>
        [TestCase]
        public void AdmArea_GetAllIncluding_Test()
        {
            /*
             * 当调用ToList()才从数据库中获取数据
             * SELECT "ID", "AreaID", "ShortName" AS "LastName", "AreaName" AS "FirstName" 
FROM "wt_adm_area"
             */
            //返回Id,Age,LastName,FirstName 字段
            Expression<Func<AdmArea, object>>[] propertySelectors = {
                p => new { Id = p.Id, AreaID = p.AreaID,LastName = p.ShortName, FirstName = p.AreaName },
            };
            var AdmAreaMapAllIncluding = _admAreaRepository.GetAllIncluding(propertySelectors).ToList();
            Assert.NotNull(AdmAreaMapAllIncluding);
        }

        /// <summary>
        /// Used to get all entities.
        /// </summary>
        /// <returns>List of all entities</returns>
        [TestCase]
        public void AdmArea_GetAllList_Test()
        {
            var admAreaAllList = _admAreaRepository.GetAllList();
            Assert.NotNull(admAreaAllList);
        }

        /// <summary>
        /// Used to get all entities.
        /// </summary>
        /// <returns>List of all entities</returns>
        [TestCase]
        public async Task AdmArea_GetAllListAsync_Test()
        {
            var admAreaAllList = await _admAreaRepository.GetAllListAsync();
            Assert.NotNull(admAreaAllList);
        }

        /// <summary>
        /// Used to get all entities based on given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">A condition to filter entities</param>
        /// <returns>List of all entities</returns>
        [TestCase]
        public void AdmArea_GetList_Predicate_Test()
        {
            /*exec sp_executesql N'SELECT "Id", "AreaID", "AreaName", "ParentId", "ShortName", "LevelType", "CityCode", "ZipCode", "AreaNamePath", "AreaIDPath", "Lng", "Lat", "PinYin", "ShortPinYin", "PYFirstLetter", "SortOrder", "Status", "CreateTime", "CreateUser", "UpdateTime", "UpdateUser", "DeleteFlag", "DeleteTime", "DeleteUser" 
FROM "wt_adm_area"
WHERE (("Id" >= @0) AND ("Id" < @1)) AND ("DeleteFlag" <> @2)
ORDER BY "AreaID", "Id" DESC',N'@0 int,@1 int,@2 bit',@0=100,@1=105,@2=1*/
            Expression<Func<AdmArea, bool>> predicate = x => x.Id >= 100 && x.Id < 105;
            Action<Orderable<AdmArea>> orderAction = (o =>
            {
                o.Desc(x => x.AreaID).Asc(x => x.CreateUser);
            });
            var result = _admAreaRepository.GetList(predicate,
                new OrderByExpression<AdmArea, string>(u => u.AreaID),    // a string, asc
    new OrderByExpression<AdmArea, int>(u => u.Id, true));
            Console.WriteLine(Utility.Json.JsonSerializationHelper.SerializeByDefault(result));
            Assert.NotNull(result);
        }

        /// <summary>
        /// Used to get all entities based on given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">A condition to filter entities</param>
        /// <returns>List of all entities</returns>
        [TestCase]
        public async Task AdmArea_GetListAsyncAsync_Predicate_Test()
        {
            /*exec sp_executesql N'SELECT "Id", "AreaID", "AreaName", "ParentId", "ShortName", "LevelType", "CityCode", "ZipCode", "AreaNamePath", "AreaIDPath", "Lng", "Lat", "PinYin", "ShortPinYin", "PYFirstLetter", "SortOrder", "Status", "CreateTime", "CreateUser", "UpdateTime", "UpdateUser", "DeleteFlag", "DeleteTime", "DeleteUser" 
FROM "wt_adm_area"
WHERE (("Id" >= @0) AND ("Id" < @1)) AND ("DeleteFlag" <> @2)
ORDER BY "AreaID", "Id" DESC',N'@0 int,@1 int,@2 bit',@0=100,@1=105,@2=1*/
            Expression<Func<AdmArea, bool>> predicate = x => x.Id >= 100 && x.Id < 105;
            Action<Orderable<AdmArea>> orderAction = (o =>
            {
                o.Desc(x => x.AreaID).Asc(x => x.CreateUser);
            });
            var result =await _admAreaRepository.GetListAsync(predicate,
                new OrderByExpression<AdmArea, string>(u => u.AreaID),    // a string, asc
    new OrderByExpression<AdmArea, int>(u => u.Id, true));
            Console.WriteLine(Utility.Json.JsonSerializationHelper.SerializeByDefault(result));
            Assert.NotNull(result);
        }

        /// <summary>
        /// Used to run a query over entire entities.
        /// <see cref="UnitOfWorkAttribute"/> attribute is not always necessary (as opposite to <see cref="GetAll"/>)
        /// if <paramref name="queryMethod"/> finishes IQueryable with ToList, FirstOrDefault etc..
        /// </summary>
        /// <typeparam name="T">Type of return value of this method</typeparam>
        /// <param name="queryMethod">This method is used to query over entities</param>
        /// <returns>Query result</returns>
        [TestCase]
        public void AdmArea_Query_Test()
        {
            //exec sp_executesql N'select AdmAreaMap0_.Id as Id1_0_, AdmAreaMap0_.AreaID as AreaID2_0_, AdmAreaMap0_.AreaName as AreaNa3_0_, AdmAreaMap0_.ParentId as Parent4_0_, AdmAreaMap0_.ShortName as ShortN5_0_, AdmAreaMap0_.LevelType as LevelT6_0_, AdmAreaMap0_.CityCode as CityCo7_0_, AdmAreaMap0_.ZipCode as ZipCod8_0_, AdmAreaMap0_.AreaNamePath as AreaNa9_0_, AdmAreaMap0_.AreaIDPath as AreaI10_0_, AdmAreaMap0_.Lng as Lng11_0_, AdmAreaMap0_.Lat as Lat12_0_, AdmAreaMap0_.PinYin as PinYi13_0_, AdmAreaMap0_.ShortPinYin as Short14_0_, AdmAreaMap0_.PYFirstLetter as PYFir15_0_, AdmAreaMap0_.SortOrder as SortO16_0_, AdmAreaMap0_.Status as Statu17_0_, AdmAreaMap0_.CreateTime as Creat18_0_, AdmAreaMap0_.CreateUser as Creat19_0_, AdmAreaMap0_.UpdateTime as Updat20_0_, AdmAreaMap0_.UpdateUser as Updat21_0_ from wt_adm_area AdmAreaMap0_ where AdmAreaMap0_.Id>=@p0 and AdmAreaMap0_.Id<@p1 ORDER BY CURRENT_TIMESTAMP OFFSET 0 ROWS FETCH FIRST 1 ROWS ONLY',N'@p0 int,@p1 int',@p0=100,@p1=105
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            Func<IQueryable<AdmArea>, AdmArea> queryMethod = q => q.Where(x => x.Id ==insertResult.Id).FirstOrDefault();
            var result = _admAreaRepository.Query(queryMethod);
            Assert.NotNull(result);
            //Console.WriteLine(result.AreaID);
        }
        /// <summary>
        /// Used to get all entities based on given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">A condition to filter entities</param>
        /// <returns>List of all entities</returns>
        [TestCase]
        public void AdmArea_GetPaggingList_Predicate_Test()
        {
            /*exec sp_executesql N'SELECT * FROM (SELECT  "Id", "AreaID", "AreaName", "ParentId", "ShortName", "LevelType", "CityCode", "ZipCode", "AreaNamePath", "AreaIDPath", "Lng", "Lat", "PinYin", "ShortPinYin", "PYFirstLetter", "SortOrder", "Status", "CreateTime", "CreateUser", "UpdateTime", "UpdateUser", "DeleteFlag", "DeleteTime", "DeleteUser", ROW_NUMBER() OVER (
ORDER BY "AreaID", "Id" DESC) As RowNum  
FROM "wt_adm_area"
WHERE (("Id" >= @0) AND ("Id" < @1)) AND ("DeleteFlag" <> @2)) AS RowConstrainedResult WHERE RowNum > 5 AND RowNum <= 10',N'@0 int,@1 int,@2 bit',@0=100,@1=200,@2=1 */
            Expression<Func<AdmArea, bool>> predicate = x => x.Id >= 100 && x.Id < 200;
            Action<Orderable<AdmArea>> orderAction = (o =>
            {
                o.Desc(x => x.AreaID).Asc(x => x.CreateUser);
            });
            var pageIndex = 2;
            var pageSize = 5;
            IOrderByExpression<AdmArea>[] orderByExpressions = {
                  new OrderByExpression<AdmArea, string>(u => u.AreaID),    // a string, asc
                new OrderByExpression<AdmArea, int>(u => u.Id, true)
            };
            var result = _admAreaRepository.GetPaggingList(predicate,
                pageIndex,
                pageSize,
               orderByExpressions);
            //Console.WriteLine(Utility.Json.JsonSerializationHelper.SerializeByDefault(result));
            Assert.NotNull(result);
        }
        /// <summary>
        /// Used to get all entities based on given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">A condition to filter entities</param>
        /// <returns>List of all entities</returns>
        [TestCase]
        public async Task AdmArea_GetPaggingListAsync_Predicate_Test()
        {
            /*exec sp_executesql N'SELECT * FROM (SELECT  "Id", "AreaID", "AreaName", "ParentId", "ShortName", "LevelType", "CityCode", "ZipCode", "AreaNamePath", "AreaIDPath", "Lng", "Lat", "PinYin", "ShortPinYin", "PYFirstLetter", "SortOrder", "Status", "CreateTime", "CreateUser", "UpdateTime", "UpdateUser", "DeleteFlag", "DeleteTime", "DeleteUser", ROW_NUMBER() OVER (
ORDER BY "AreaID", "Id" DESC) As RowNum  
FROM "wt_adm_area"
WHERE (("Id" >= @0) AND ("Id" < @1)) AND ("DeleteFlag" <> @2)) AS RowConstrainedResult WHERE RowNum > 10 AND RowNum <= 15',N'@0 int,@1 int,@2 bit',@0=100,@1=200,@2=1 */
            Expression<Func<AdmArea, bool>> predicate = x => x.Id >= 100 && x.Id < 200;
            Action<Orderable<AdmArea>> orderAction = (o =>
            {
                o.Desc(x => x.AreaID).Asc(x => x.CreateUser);
            });
            var pageIndex = 3;
            var pageSize = 5;
            IOrderByExpression<AdmArea>[] orderByExpressions = {
                  new OrderByExpression<AdmArea, string>(u => u.AreaID),    // a string, asc
                new OrderByExpression<AdmArea, int>(u => u.Id, true)
            };
            var result =await _admAreaRepository.GetPaggingListAsync(predicate,
                pageIndex,
                pageSize,
               orderByExpressions);
            //Console.WriteLine(Utility.Json.JsonSerializationHelper.SerializeByDefault(result));
            Assert.NotNull(result);
        }
        #endregion
        #region Insert

        /// <summary>
        /// Inserts a new entity.
        /// </summary>
        /// <param name="entity">Inserted entity</param>
        [TestCase]
        public void AdmArea_Insert_Test()
        {

            /*
            exec sp_executesql N'INSERT INTO "wt_adm_area" ("AreaID","AreaName","ParentId","ShortName","LevelType","CityCode","ZipCode","AreaNamePath","AreaIDPath","lng","Lat","PinYin","ShortPinYin","PYFirstLetter","SortOrder","Status","CreateTime","CreateUser","UpdateTime","UpdateUser") VALUES (@AreaID,@AreaName,@ParentId,@ShortName,@LevelType,@CityCode,@ZipCode,@AreaNamePath,@AreaIDPath,@lng,@Lat,@PinYin,@ShortPinYin,@PYFirstLetter,@SortOrder,@Status,@CreateTime,@CreateUser,@UpdateTime,@UpdateUser); SELECT SCOPE_IDENTITY()',N'@AreaID varchar(6),@AreaName varchar(15),@ParentId varchar(6),@ShortName varchar(8),@LevelType tinyint,@CityCode varchar(8000),@ZipCode varchar(8000),@AreaNamePath varchar(20),@AreaIDPath varchar(13),@lng decimal(1,0),@Lat decimal(1,0),@PinYin varchar(8000),@ShortPinYin varchar(8000),@PYFirstLetter varchar(8000),@SortOrder int,@Status tinyint,@CreateTime datetime,@CreateUser int,@UpdateTime datetime,@UpdateUser int',@AreaID='164386',@AreaName='测试地区名称646',@ParentId='100000',@ShortName='地区简称',@LevelType=0,@CityCode='',@ZipCode='',@AreaNamePath='中国,测试地区名称646',@AreaIDPath='100000,164386',@lng=0,@Lat=0,@PinYin=NULL,@ShortPinYin='',@PYFirstLetter='',@SortOrder=84,@Status=0,@CreateTime='2017-06-22 15:27:12.497',@CreateUser=5381,@UpdateTime='2017-06-22 15:27:12.497',@UpdateUser=363
             */
            var entity = CreateAdmArea();
            var result = _admAreaRepository.Insert(entity);
            Console.WriteLine(result.Id);
            Assert.AreEqual(entity.AreaID, result.AreaID);
        }

        /// <summary>
        /// Inserts a new entity.
        /// </summary>
        /// <param name="entity">Inserted entity</param>
        [TestCase]
        public async Task AdmArea_InsertAsync_Test()
        {
            var entity = CreateAdmArea();
            var result = await _admAreaRepository.InsertAsync(entity);
            Assert.AreEqual(entity.AreaID, result.AreaID);
        }

        /// <summary>
        /// Inserts a new entity and gets it's Id.
        /// It may require to save current unit of work
        /// to be able to retrieve id.
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Id of the entity</returns>
        [TestCase]
        public void AdmArea_InsertAndGetId_Test()
        {
            //exec sp_executesql N'INSERT INTO wt_adm_area (AreaID, AreaName, ParentId, ShortName, LevelType, CityCode, ZipCode, AreaNamePath, AreaIDPath, lng, Lat, PinYin, ShortPinYin, PYFirstLetter, SortOrder, Status, CreateTime, CreateUser, UpdateTime, UpdateUser) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17, @p18, @p19); select SCOPE_IDENTITY()',N'@p0 nvarchar(4000),@p1 nvarchar(4000),@p2 nvarchar(4000),@p3 nvarchar(4000),@p4 tinyint,@p5 nvarchar(4000),@p6 nvarchar(4000),@p7 nvarchar(4000),@p8 nvarchar(4000),@p9 decimal(28,5),@p10 decimal(28,5),@p11 nvarchar(4000),@p12 nvarchar(4000),@p13 nvarchar(4000),@p14 int,@p15 tinyint,@p16 datetime,@p17 int,@p18 datetime,@p19 int',@p0=N'151282',@p1=N'测试地区名称187',@p2=N'100000',@p3=N'地区简称',@p4=0,@p5=NULL,@p6=NULL,@p7=N'中国,测试地区名称187',@p8=N'100000,151282',@p9=0,@p10=0,@p11=NULL,@p12=NULL,@p13=NULL,@p14=52,@p15=0,@p16='2017-06-21 17:46:14',@p17=5884,@p18='2017-06-21 17:46:14',@p19=4616
            var entity = CreateAdmArea();
            var result = _admAreaRepository.InsertAndGetId(entity);
            Assert.Greater(result, 0);
            Console.WriteLine("AdmArea,id:" + entity.Id);
        }

        /// <summary>
        /// Inserts a new entity and gets it's Id.
        /// It may require to save current unit of work
        /// to be able to retrieve id.
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Id of the entity</returns>
        [TestCase]
        public async Task AdmArea_InsertAndGetIdAsync_Test()
        {
            var entity = CreateAdmArea();
            var result = await _admAreaRepository.InsertAndGetIdAsync(entity);
            Assert.Greater(result, 0);
        }

        /// <summary>
        /// Inserts or updates given entity depending on Id's value.
        /// </summary>
        /// <param name="entity">Entity</param>
        [TestCase]
        public void AdmArea_InsertOrUpdate_Test()
        {
            /*
             * exec sp_executesql N'UPDATE "wt_adm_area" SET "AreaID"=@AreaID, "AreaName"=@AreaName, "ParentId"=@ParentId, "ShortName"=@ShortName, "LevelType"=@LevelType, "CityCode"=@CityCode, "ZipCode"=@ZipCode, "AreaNamePath"=@AreaNamePath, "AreaIDPath"=@AreaIDPath, "lng"=@lng, "Lat"=@Lat, "PinYin"=@PinYin, "ShortPinYin"=@ShortPinYin, "PYFirstLetter"=@PYFirstLetter, "SortOrder"=@SortOrder, "Status"=@Status, "CreateTime"=@CreateTime, "CreateUser"=@CreateUser, "UpdateTime"=@UpdateTime, "UpdateUser"=@UpdateUser WHERE "ID"=@ID',N'@ID int,@AreaID varchar(6),@AreaName varchar(15),@ParentId varchar(6),@ShortName varchar(8),@LevelType tinyint,@CityCode varchar(8000),@ZipCode varchar(8000),@AreaNamePath varchar(20),@AreaIDPath varchar(13),@lng decimal(1,0),@Lat decimal(1,0),@PinYin varchar(8000),@ShortPinYin varchar(8000),@PYFirstLetter varchar(8000),@SortOrder int,@Status tinyint,@CreateTime datetime,@CreateUser int,@UpdateTime datetime,@UpdateUser int',@ID=3757,@AreaID='422967',@AreaName='测试地区名称490',@ParentId='100000',@ShortName='地区简称',@LevelType=0,@CityCode='',@ZipCode='',@AreaNamePath='中国,测试地区名称490',@AreaIDPath='100000,422967',@lng=0,@Lat=0,@PinYin=NULL,@ShortPinYin='',@PYFirstLetter='',@SortOrder=34,@Status=0,@CreateTime='2017-06-22 15:27:49.847',@CreateUser=5526,@UpdateTime='2017-06-22 15:27:49.847',@UpdateUser=4190
             */
            var entity = CreateAdmArea();
            entity.Id = 3757;
            var result = _admAreaRepository.InsertOrUpdate(entity);
            Assert.AreEqual(result.AreaID, result.AreaID);
        }

        /// <summary>
        /// Inserts or updates given entity depending on Id's value.
        /// </summary>
        /// <param name="entity">Entity</param>
        [TestCase]
        public async Task AdmArea_InsertOrUpdateAsync_Test()
        {
            var entity = CreateAdmArea();
            var result = await _admAreaRepository.InsertOrUpdateAsync(entity);
            Assert.AreEqual(result.AreaID, result.AreaID);
        }

        /// <summary>
        /// Inserts or updates given entity depending on Id's value.
        /// Also returns Id of the entity.
        /// It may require to save current unit of work
        /// to be able to retrieve id.
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Id of the entity</returns>
        [TestCase]
        public void AdmArea_InsertOrUpdateAndGetId_Test()
        {
            /*
             * exec sp_executesql N'UPDATE [dbo].[wt_adm_area]
SET [AreaID] = @0, [AreaName] = @1, [ParentId] = @2, [ShortName] = @3, [LevelType] = @4, [CityCode] = @5, [ZipCode] = @6, [AreaNamePath] = @7, [AreaIDPath] = @8, [lng] = @9, [Lat] = @10, [PinYin] = NULL, [ShortPinYin] = @11, [PYFirstLetter] = @12, [SortOrder] = @13, [Status] = @14, [CreateTime] = @15, [CreateUser] = @16, [UpdateTime] = @17, [UpdateUser] = @18
WHERE ([Id] = @19)
',N'@0 varchar(50),@1 varchar(50),@2 varchar(50),@3 varchar(50),@4 tinyint,@5 varchar(50),@6 varchar(50),@7 nvarchar(500),@8 varchar(500),@9 decimal(18,2),@10 decimal(18,2),@11 varchar(20),@12 varchar(10),@13 int,@14 tinyint,@15 datetime2(7),@16 int,@17 datetime2(7),@18 int,@19 int',@0='856519',@1='测试地区名称915',@2='100000',@3='地区简称',@4=0,@5='',@6='',@7=N'中国,测试地区名称915',@8='100000,856519',@9=0,@10=0,@11='',@12='',@13=96,@14=0,@15='2017-06-21 22:10:06.9312448',@16=3811,@17='2017-06-21 22:10:06.9312448',@18=3000,@19=3756
             */
            var entity = CreateAdmArea();
            entity.Id = 3756;
            var result = _admAreaRepository.InsertOrUpdateAndGetId(entity);
            Assert.Greater(result, 0);
        }

        /// <summary>
        /// Inserts or updates given entity depending on Id's value.
        /// Also returns Id of the entity.
        /// It may require to save current unit of work
        /// to be able to retrieve id.
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Id of the entity</returns>
        [TestCase]
        public async Task AdmArea_InsertOrUpdateAndGetIdAsync_Test()
        {
            var entity = CreateAdmArea();
            var result = await _admAreaRepository.InsertOrUpdateAndGetIdAsync(entity);
            Assert.Greater(result, 0);
        }

        #endregion

        #region Update 

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="entity">Entity</param>
        [Test]
        public void AdmArea_Update_Test()
        {
            /*
             * exec sp_executesql N'UPDATE [dbo].[wt_adm_area]
SET [AreaID] = @0, [AreaName] = @1, [ParentId] = @2, [ShortName] = @3, [LevelType] = @4, [CityCode] = @5, [ZipCode] = @6, [AreaNamePath] = @7, [AreaIDPath] = @8, [lng] = @9, [Lat] = @10, [PinYin] = NULL, [ShortPinYin] = @11, [PYFirstLetter] = @12, [SortOrder] = @13, [Status] = @14, [CreateTime] = @15, [CreateUser] = @16, [UpdateTime] = @17, [UpdateUser] = @18
WHERE ([Id] = @19)
',N'@0 varchar(50),@1 varchar(50),@2 varchar(50),@3 varchar(50),@4 tinyint,@5 varchar(50),@6 varchar(50),@7 nvarchar(500),@8 varchar(500),@9 decimal(18,2),@10 decimal(18,2),@11 varchar(20),@12 varchar(10),@13 int,@14 tinyint,@15 datetime2(7),@16 int,@17 datetime2(7),@18 int,@19 int',@0='409878',@1='测试地区名称259 Update',@2='100000',@3='地区简称',@4=0,@5='',@6='',@7=N'中国,测试地区名称259',@8='100000,409878',@9=0,@10=0,@11='',@12='',@13=92,@14=0,@15='2017-06-21 22:13:50.3718472',@16=8442,@17='2017-06-21 22:13:51.2108839',@18=1430,@19=3759
             */
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            insertResult.AreaName = insertResult.AreaName + " Update";
            insertResult.UpdateTime = DateTime.Now;
            var result = _admAreaRepository.Update(insertResult);
            Assert.AreEqual(result.AreaName, insertResult.AreaName);
        }

        /// <summary>
        /// Updates an existing entity. 
        /// </summary>
        /// <param name="entity">Entity</param>
        [TestCase]
        public async Task AdmArea_UpdateAsync_Test()
        {
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            entity.AreaName += " UpdateAsync";
            entity.UpdateTime = DateTime.Now;
            var result = await _admAreaRepository.UpdateAsync(entity);
            Assert.AreEqual(result.AreaName, entity.AreaName);
        }

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="id">Id of the entity</param>
        /// <param name="updateAction">Action that can be used to change values of the entity</param>
        /// <returns>Updated entity</returns>
        [TestCase]
        public void AdmArea_Update_UpdateAction_Test()
        {
            /*
             exec sp_executesql N'UPDATE "wt_adm_area" SET "AreaID"=@AreaID, "AreaName"=@AreaName, "ParentId"=@ParentId, "ShortName"=@ShortName, "LevelType"=@LevelType, "CityCode"=@CityCode, "ZipCode"=@ZipCode, "AreaNamePath"=@AreaNamePath, "AreaIDPath"=@AreaIDPath, "lng"=@lng, "Lat"=@Lat, "PinYin"=@PinYin, "ShortPinYin"=@ShortPinYin, "PYFirstLetter"=@PYFirstLetter, "SortOrder"=@SortOrder, "Status"=@Status, "CreateTime"=@CreateTime, "CreateUser"=@CreateUser, "UpdateTime"=@UpdateTime, "UpdateUser"=@UpdateUser WHERE "ID"=@ID',N'@ID int,@AreaID varchar(6),@AreaName varchar(28),@ParentId varchar(6),@ShortName varchar(8),@LevelType tinyint,@CityCode varchar(8000),@ZipCode varchar(8000),@AreaNamePath varchar(20),@AreaIDPath varchar(13),@lng decimal(10,10),@Lat decimal(10,10),@PinYin varchar(8000),@ShortPinYin varchar(8000),@PYFirstLetter varchar(8000),@SortOrder int,@Status tinyint,@CreateTime datetime,@CreateUser int,@UpdateTime datetime,@UpdateUser int',@ID=3830,@AreaID='340888',@AreaName='测试地区名称145 UpdateAction',@ParentId='100000',@ShortName='地区简称',@LevelType=0,@CityCode='',@ZipCode='',@AreaNamePath='中国,测试地区名称145',@AreaIDPath='100000,340888',@lng=0,@Lat=0,@PinYin=NULL,@ShortPinYin='',@PYFirstLetter='',@SortOrder=37,@Status=0,@CreateTime='2017-06-22 16:53:27.520',@CreateUser=3207,@UpdateTime='2017-06-22 16:53:27.520',@UpdateUser=626
             */
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            var result = _admAreaRepository.Update(entity.Id,
                (a) =>
                {
                    a.AreaName += " UpdateAction";
                    return a;
                });
            Assert.AreEqual(result.AreaName, entity.AreaName + " UpdateAction");
        }

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="id">Id of the entity</param>
        /// <param name="updateAction">Action that can be used to change values of the entity</param>
        /// <returns>Updated entity</returns>
        [TestCase]
        public async Task AdmArea_UpdateAsync_UpdateAction_Test()
        {
            //测试不通过
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            var result = await _admAreaRepository.UpdateAsync(entity.Id,
               (a) =>
               {
                   return Task.Factory.StartNew(() =>
                   {
                       a.AreaName += " Async UpdateAction";
                       return a;
                   });
               }
              );
            Assert.AreEqual(result.AreaName, entity.AreaName + " Async UpdateAction");
        }

        #endregion 

        #region Delete 

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="entity">Entity to be deleted</param>
        [Test]
        public void AdmArea_Delete_Test()
        {
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            //System.Threading.Thread.Sleep(500);
            _admAreaRepository.Delete(entity);
        }

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="entity">Entity to be deleted</param>
        [Test]
        public async Task AdmArea_DeleteAsync_Test()
        {
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            await _admAreaRepository.DeleteAsync(entity);
        }

        /// <summary>
        /// Deletes an entity by primary key.
        /// </summary>
        /// <param name="id">Primary key of the entity</param>
        [Test]
        public void AdmArea_Delete_TPrimaryKey_Test()
        {
            /*
             exec sp_executesql N'DELETE FROM "wt_adm_area" WHERE "ID" = @0',N'@0 int',@0=0
             */
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            _admAreaRepository.Delete(entity.Id);
        }

        /// <summary>
        /// Deletes an entity by primary key.
        /// </summary>
        /// <param name="id">Primary key of the entity</param>
        [Test]
        public async Task AdmArea_DeleteAsync_TPrimaryKey_Test()
        {
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            await _admAreaRepository.DeleteAsync(entity.Id);
        }

        /// <summary>
        /// Deletes many entities by function.
        /// Notice that: All entities fits to given predicate are retrieved and deleted.
        /// This may cause major performance problems if there are too many entities with
        /// given predicate.
        /// </summary>
        /// <param name="predicate">A condition to filter entities</param>
        [Test]
        public void AdmArea_Delete_Predicate_Test()
        {
            /*
            exec sp_executesql N'DELETE FROM "wt_adm_area" WHERE (("ID" = @0) AND ("AreaID" = @1))',N'@0 int,@1 varchar(8000)',@0=0,@1='710087'
             */
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            _admAreaRepository.Delete(x => x.Id == entity.Id && x.AreaID == entity.AreaID);
        }

        /// <summary>
        /// Deletes many entities by function.
        /// Notice that: All entities fits to given predicate are retrieved and deleted.
        /// This may cause major performance problems if there are too many entities with
        /// given predicate.
        /// </summary>
        /// <param name="predicate">A condition to filter entities</param>
        [Test]
        public async Task AdmArea_DeleteAsync_Predicate_Test()
        {
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            await _admAreaRepository.DeleteAsync(x => x.Id == entity.Id && x.AreaID == entity.AreaID);
        }

        #endregion 
        #region Aggregates Tests

        /// <summary>
        /// Gets count of all entities in this repository.
        /// </summary>
        /// <returns>Count of entities</returns>
        [Test]
        public void AdmArea_Count_Test()
        {
            /*
             SELECT COUNT(*) 
FROM "wt_adm_area"
           */
            var result = _admAreaRepository.Count();
            Assert.GreaterOrEqual(result, 0);
            Console.WriteLine(result);
        }

        /// <summary>
        /// Gets count of all entities in this repository.
        /// </summary>
        /// <returns>Count of entities</returns>
        [Test]
        public async Task AdmArea_CountAsync_Test()
        {
            /*
              SELECT COUNT(*) 
FROM "wt_adm_area"
             */
            var result = await _admAreaRepository.CountAsync();
            Assert.GreaterOrEqual(result, 0);
            Console.WriteLine(result);
        }

        /// <summary>
        /// Gets count of all entities in this repository based on given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">A method to filter count</param>
        /// <returns>Count of entities</returns>
        [Test]
        public void AdmArea_Count_Predicate_Test()
        {
            /*exec sp_executesql N'SELECT COUNT(*) 
FROM "wt_adm_area"
WHERE (("ID" > @0) AND ("ID" < @1))',N'@0 int,@1 int',@0=100,@1=1000
    */
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            Expression<Func<AdmArea, bool>> predicate = x => x.Id ==insertResult.Id;
            var result = _admAreaRepository.Count(predicate);
            Assert.GreaterOrEqual(result, 1);
        }

        /// <summary>
        /// Gets count of all entities in this repository based on given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">A method to filter count</param>
        /// <returns>Count of entities</returns>
        [Test]
        public async Task AdmArea_CountAsync_Predicate_Test()
        {
            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            Expression<Func<AdmArea, bool>> predicate = x => x.Id==entity.Id;
            var result = await _admAreaRepository.CountAsync(predicate);
            Assert.GreaterOrEqual(result, 1);
        }

        /// <summary>
        /// Gets count of all entities in this repository (use if expected return value is greather than <see cref="int.MaxValue"/>.
        /// </summary>
        /// <returns>Count of entities</returns>
        [Test]
        public void AdmArea_LongCount_Test()
        {
            var result = _admAreaRepository.LongCount();
            Assert.GreaterOrEqual(result, 0);
            Console.WriteLine(result);
        }

        /// <summary>
        /// Gets count of all entities in this repository (use if expected return value is greather than <see cref="int.MaxValue"/>.
        /// </summary>
        /// <returns>Count of entities</returns>
        [Test]
        public async Task AdmArea_LongCountAsync_Test()
        {
            var result = await _admAreaRepository.LongCountAsync();
            Assert.GreaterOrEqual(result, 0);
            Console.WriteLine(result);
        }

        /// <summary>
        /// Gets count of all entities in this repository based on given <paramref name="predicate"/>
        /// (use this overload if expected return value is greather than <see cref="int.MaxValue"/>).
        /// </summary>
        /// <param name="predicate">A method to filter count</param>
        /// <returns>Count of entities</returns>
        [Test]
        public void AdmArea_LongCount_Predicate_Test()
        {
            /*
             SELECT 
                [GroupBy1].[A1] AS [C1]
                FROM ( SELECT 
                    COUNT_BIG(1) AS [A1]
                    FROM [dbo].[wt_adm_area] AS [Extent1]
                    WHERE ([Extent1].[Id] > 100) AND ([Extent1].[Id] < 1000)
                )  AS [GroupBy1]
             */

            var entity = CreateAdmArea();
            var insertResult = _admAreaRepository.Insert(entity);
            Expression<Func<AdmArea, bool>> predicate = x => x.Id == entity.Id;
            var result = _admAreaRepository.LongCount(predicate);
            Assert.GreaterOrEqual(result, 1);
        }

        /// <summary>
        /// Gets count of all entities in this repository based on given <paramref name="predicate"/>
        /// (use this overload if expected return value is greather than <see cref="int.MaxValue"/>).
        /// </summary>
        /// <param name="predicate">A method to filter count</param>
        /// <returns>Count of entities</returns>
        [Test]
        public async Task AdmArea_LongCountAsync_Predicate_Test()
        {

            Expression<Func<AdmArea, bool>> predicate = x => x.Id > 100 && x.Id < 1000;
            var result = await _admAreaRepository.LongCountAsync(predicate);
            Assert.GreaterOrEqual(result, 10);
        }
        #endregion  Aggregates

    }
}
