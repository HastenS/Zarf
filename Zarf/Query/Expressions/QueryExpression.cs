using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Zarf.Entities;
using Zarf.Extensions;
using Zarf.Mapping;
using Zarf.Query.ExpressionVisitors;
using System.Linq;

namespace Zarf.Query.Expressions
{
    public class QueryExpression : FromTableExpression
    {
        public List<Expression> ProjectionExpressionCollection
        {
            get
            {
                return ProjectionCollection.Select(item => item.Expression).ToList();
            }
        }

        /// <summary>
        /// ��ѯͶӰ
        /// </summary>
        public List<Projection> ProjectionCollection { get; }

        /// <summary>
        /// ������
        /// </summary>
        public List<JoinExpression> Joins { get; }

        /// <summary>
        /// ����������ѯ����
        /// Union Except etc...
        /// </summary>
        public List<SetsExpression> Sets { get; }

        /// <summary>
        /// ����
        /// </summary>
        public List<OrderExpression> Orders { get; }

        /// <summary>
        /// ����
        /// </summary>
        public List<GroupExpression> Groups { get; }

        /// <summary>
        /// ����
        /// </summary>
        public WhereExperssion Where { get; set; }

        /// <summary>
        /// ȥ��
        /// </summary>
        public bool IsDistinct { get; set; }

        /// <summary>
        /// ��������
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// ��ѯ���ƫ����
        /// </summary>
        public SkipExpression Offset { get; set; }

        /// <summary>
        /// Ϊ��ʱ����Ĭ��ֵ
        /// </summary>
        public bool DefaultIfEmpty { get; set; }

        /// <summary>
        /// ��ʾһ���Ӳ�ѯ
        /// </summary>
        public QueryExpression SubQuery { get; protected set; }

        public EntityResult Result { get; set; }

        public QueryExpression(Type entityType, string alias = "")
            : base(entityType, alias)
        {
            Sets = new List<SetsExpression>();
            Joins = new List<JoinExpression>();
            Orders = new List<OrderExpression>();
            Groups = new List<GroupExpression>();
            ProjectionCollection = new List<Projection>();
        }

        public QueryExpression PushDownSubQuery(string fromTableAlias, Func<QueryExpression, QueryExpression> subQueryHandle = null)
        {
            var query = new QueryExpression(Type, fromTableAlias)
            {
                SubQuery = this,
                Table = null,
                DefaultIfEmpty = DefaultIfEmpty
            };

            DefaultIfEmpty = false;
            Parent = query;
            return subQueryHandle != null ? subQueryHandle(query) : query;
        }

        public void AddJoin(JoinExpression table)
        {
            Joins.Add(table);
        }

        public void AddProjections(IEnumerable<Projection> projections)
        {
            ProjectionCollection.Clear();
            ProjectionCollection.AddRange(projections);
        }

        public void AddWhere(Expression predicate)
        {
            if (predicate == null)
            {
                return;
            }

            if (Where == null)
            {
                Where = new WhereExperssion(predicate);
            }
            else
            {
                Where.Combine(predicate);
            }
        }

        /// <summary>
        /// �Ƿ�һ���ղ�ѯ
        /// ����һ��Table
        /// </summary>
        /// <returns></returns>
        public bool IsEmptyQuery()
        {
            return
                !IsDistinct &&
                //!DefaultIfEmpty &&
                Where == null &&
                Offset == null &&
                SubQuery == null &&
                ProjectionCollection.Count == 0 &&
                Orders.Count == 0 &&
                Groups.Count == 0 &&
                Sets.Count == 0 &&
                Joins.Count == 0 &&
                Limit == 0;
        }
    }
}