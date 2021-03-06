﻿using Entities;
using System;
using Zarf;
using Zarf.Metadata.Entities;

namespace ConsoleApp
{
    public class QueryDemo
    {
        public static void Query(DbContext db)
        {
            //foreach
            foreach (var item in db.Query<User>())
            {
                Console.WriteLine(item);
            }

            ToList(db);

            AsEnumberable(db);

            First(db);

            Single(db);

            Skip(db);

            Aggreate(db);

            Take(db);

            Where(db);

            OrderBy(db);

            GroupBy(db);

            Union(db);

            Except(db);

            Intersect(db);

            All(db);

            Any(db);

            Select(db);

            Distinct(db);

            Join(db);

            FunctionQuery(db);
        }

        public static void Distinct(DbContext db)
        {
            Console.WriteLine(" Discinct ");

            db.Query<User>().Distinct();
        }

        public static void Single(DbContext db)
        {
            var single = db.Query<User>().Single(i => i.Id == 3);
            var singleOrDefault = db.Query<User>().SingleOrDefault(i => i.Id == 3);

            Console.WriteLine(" Single Id =3 ");
            Console.WriteLine(single);
            Console.WriteLine("SingleOrDefault Id=3");
            Console.WriteLine(singleOrDefault);
        }

        public static void First(DbContext db)
        {
            var first = db.Query<User>().First();
            var firstOrDefault = db.Query<User>().FirstOrDefault();

            Console.WriteLine(" First  ");
            Console.WriteLine(first);
            Console.WriteLine(" FistOrDefault ");
            Console.WriteLine(firstOrDefault);

            first = db.Query<User>().First(i => i.Id == 3);
            firstOrDefault = db.Query<User>().FirstOrDefault(i => i.Id == 3);

            Console.WriteLine(" Fist Id =3 ");
            Console.WriteLine(first);
            Console.WriteLine("FistOrDefault Id=3");
            Console.WriteLine(firstOrDefault);
        }

        public static void ToList(DbContext db)
        {
            var users = db.Query<User>().ToList();
        }

        public static void AsEnumberable(DbContext db)
        {
            var users = db.Query<User>().AsEnumerable();
        }

        public static void Skip(DbContext db)
        {
            var users = db.Query<User>().Skip(2);

            Console.WriteLine(" Skip 2");

            foreach (var item in users)
            {
                Console.WriteLine(item);
            }
        }

        public static void Take(DbContext db)
        {
            var users = db.Query<User>().Take(10);

            Console.WriteLine("Take 10 ");

            foreach (var item in users)
            {
                Console.WriteLine(item);
            }
        }

        public static void Aggreate(DbContext db)
        {
            var count = db.Query<User>().Count();

            var longCount = db.Query<User>().LongCount();

            var sumAge = db.Query<User>().Sum(i => i.Age);

            var avgAge = db.Query<User>().Average(i => i.Age);

            var maxId = db.Query<User>().Max(i => i.Id);

            var minId = db.Query<User>().Min(i => i.Id);

            Console.WriteLine("User Count");
            Console.WriteLine(count);

            Console.WriteLine("Sum Age");
            Console.Write(sumAge);

            Console.WriteLine(" Max Id ");
            Console.WriteLine(maxId);

            Console.WriteLine(" Min Id ");
            Console.WriteLine(minId);
        }

        public static void Where(DbContext db)
        {
            Console.WriteLine("Where Id>10");

            var usersIdMoreThanTen = db.Query<User>().Where(i => i.Id > 10);

            foreach (var item in usersIdMoreThanTen)
            {
                Console.WriteLine(item);
            }
        }

        public static void OrderBy(DbContext db)
        {
            var orderByIdDesc = db.Query<User>().OrderByDescending(i => i.Id);

            Console.WriteLine(" Order By Id Desc ");

            foreach (var item in orderByIdDesc)
            {
                Console.WriteLine(item);
            }
        }

        /// <summary>
        /// GroupBy OrderBy还没实现new {item.Id,item.Age} ,item=>item
        /// </summary>
        /// <param name="db"></param>
        public static void GroupBy(DbContext db)
        {
            var groupBy = db.Query<User>().Select(i => new { i.Id }).GroupBy(i => i.Id);

            Console.WriteLine(" Select Id GroupBy Id ");

            foreach (var item in groupBy)
            {
                Console.WriteLine(item);
            }
        }

        public static void Union(DbContext db)
        {
            var concats = db.Query<User>().Concat(db.Query<User>());

            Console.WriteLine("CONCAT 包含重复项 ");

            foreach (var item in concats)
            {
                Console.WriteLine(item);
            }

            var unions = db.Query<User>().Union(db.Query<User>());

            Console.WriteLine("UNION 不包含重复项 ");

            foreach (var item in unions)
            {
                Console.WriteLine(item);
            }
        }

        public static void Except(DbContext db)
        {
            var excepts = db.Query<User>().Concat(db.Query<User>().Where(i => i.Id > 2));

            Console.WriteLine("Excepts ");

            foreach (var item in excepts)
            {
                Console.WriteLine(item);
            }
        }

        public static void Intersect(DbContext db)
        {
            var intersects = db.Query<User>().Intersect(db.Query<User>().Where(i => i.Id > 2));

            Console.WriteLine("Excepts ");

            foreach (var item in intersects)
            {
                Console.WriteLine(item);
            }
        }

        public static void All(DbContext db)
        {
            Console.WriteLine(" All Id > 0 ");
            Console.WriteLine(db.Query<User>().All(u => u.Id > 0));
        }

        public static void Any(DbContext db)
        {
            Console.WriteLine(" Any Id > 10 ");
            Console.WriteLine(db.Query<User>().Any(u => u.Id > 10));
        }

        public static void Select(DbContext db)
        {
            Console.WriteLine("Select Id,Age ");

            var idAndAges = db.Query<User>().Select(i => new { i.Id, i.Age });

            foreach (var item in idAndAges)
            {
                Console.WriteLine(item);
            }
        }

        /// <summary>
        /// Join核心实现
        /// InnerJoin LeftJoin CrossJoin RightJoin FullJoin 扩展方法
        /// </summary>
        /// <param name="db"></param>
        public static void Join(DbContext db)
        {
            var us = db.Query<User>();
            var os = db.Query<Order>();

            var innerJoins = us
                .Join(os, (u, o) => u.Id == o.UserId, JoinType.Inner)
                .Select((u, o) => new { u.Id, o.Goods });

            var leftJoins = us
                .LeftJoin(os, (u, o) => u.Id == o.UserId)
                .Select((u, o) => new { u.Id, o.Goods });

            Console.WriteLine(" User Inner Join Order Id=Id");

            foreach (var item in innerJoins)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine(" User Left Join Order Id = Id");

            foreach (var item in leftJoins)
            {
                Console.WriteLine(item);
            }
        }

        /// <summary>
        /// 子查询
        /// 创建元素的委托难以缓存
        /// </summary>
        /// <param name="db"></param>
        public static void SubQuery(DbContext db)
        {
            //子查询中不能返回IQuery接口类型
            //必须在每一个查询的结尾调用ToList/AsEnumerable
            //(AsEnumerable可以将子查询延迟到第一条记录访问时查询),
            //First,FirstOrDefault,Single/OrDefault,Sum,Aervage,Count,Max,Min之一
            //每一个子查询尽量引用外部查询条件过滤,减少查询数量量
            //如下面的Orders,MaxUserId
            //聚合类的查询会合并到外层查询中,非聚合则内存中过滤
            var uos = db.Query<User>()
                .Where(i => i.Id < 10)
                .Select(i => new
                {
                    UserId = i.Id,
                    Orders = db.Query<Order>().Where(o => o.UserId == i.Id).ToList(),
                });

            Console.WriteLine(" Sub Query ");

            foreach (var item in uos)
            {
                Console.WriteLine(item);
            }
        }

        public static void FunctionQuery(DbContext db)
        {
            var idAdd2 = db.Query<User>().Where(i => i.Id.Add(2) < 10)
                .Select(i => new
                {
                    IdAdd3 = i.Id.Add(3),
                    Name = i.Name
                });

            Console.WriteLine("Function ");

            foreach (var item in idAdd2)
            {
                Console.WriteLine(item);
            }
        }
    }
}
