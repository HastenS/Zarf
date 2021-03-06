﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Zarf.Extensions;
using Zarf.Metadata.Entities;
using Zarf.Query.Expressions;
using Zarf.Query.Internals;

namespace Zarf.Query.Visitors
{
    /// <summary>
    /// 聚合Visitor
    /// </summary>
    public class AggreateExpressionVisitor : QueryExpressionVisitor
    {
        /// <summary>
        /// 已被处理的查询
        /// </summary>
        protected List<SelectExpression> HandledSelects { get; }

        public SelectExpression Select { get; }

        public AggreateExpressionVisitor(IQueryContext context, SelectExpression select) : base(context)
        {
            Select = select;
            HandledSelects = new List<SelectExpression>();
        }

        public override Expression Visit(Expression exp)
        {
            if (exp.NodeType == ExpressionType.MemberAccess)
            {
                return VisitMember(exp.As<MemberExpression>());
            }

            return base.Visit(exp);
        }

        /// <summary>
        /// 聚合引用其他表的列,拷贝引用表
        /// </summary>
        protected override Expression VisitMember(MemberExpression mem)
        {
            var queryModel = QueryContext.ModelMapper.GetValue(mem.Expression);
            if (queryModel != null)
            {
                if (!Select.ContainsSelectExpression(queryModel.Select))
                {
                    throw new NotImplementedException("can not aggregate a column from outer refrence!");
                }

                while (queryModel != null)
                {
                    if (queryModel.Model.Type != mem.Member.DeclaringType)
                    {
                        queryModel = queryModel.Previous;
                    }

                    var binding = QueryContext.BindingMaper.GetValue(Expression.MakeMemberAccess(queryModel.Model, mem.Member));
                    var mappedExpression = QueryContext.ExpressionMapper.GetValue(binding);
                    if (mappedExpression != null)
                    {
                        binding = mappedExpression;
                    }

                    if (binding == null)
                    {
                        queryModel = queryModel?.Previous;
                        continue;
                    }

                    if (binding is AliasExpression alis)
                    {
                        if (!(alis.Expression is SelectExpression))
                        {
                            return alis.Expression;
                        }

                        //引用为表,则说明这是子查询中的聚合
                        return new ColumnExpression(Select, new Column(alis.Alias), alis.Type);
                    }
                    else
                    {
                        return binding;
                    }

                    throw new Exception("find aggregage refrence colum faild!");
                }
            }

            var expression = base.Visit(mem);

            return expression.As<AliasExpression>()?.Expression ?? expression;
        }
    }
}
