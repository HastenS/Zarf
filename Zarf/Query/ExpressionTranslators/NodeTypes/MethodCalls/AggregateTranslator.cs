﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zarf.Entities;
using Zarf.Extensions;
using Zarf.Query.Expressions;
using Zarf.Query.ExpressionVisitors;

namespace Zarf.Query.ExpressionTranslators.NodeTypes.MethodCalls
{
    public class AggregateTranslator : Translator<MethodCallExpression>
    {
        public static IEnumerable<MethodInfo> SupprotedMethods { get; }

        static AggregateTranslator()
        {
            var methods = new[] { "Max", "Sum", "Min", "Average", "Count", "LongCount" };
            SupprotedMethods = ReflectionUtil.QueryableMethods.Where(item => methods.Contains(item.Name));
        }

        public AggregateTranslator(IQueryContext queryContext, IQueryCompiler queryCompiper) : base(queryContext, queryCompiper)
        {

        }

        public override Expression Translate(MethodCallExpression methodCall)
        {
            var query = GetCompiledExpression<QueryExpression>(methodCall.Arguments[0]);

            if (query.Sets.Count != 0)
            {
                query = query.PushDownSubQuery(Context.Alias.GetNewTable());
            }

            if (methodCall.Arguments.Count == 1)
            {
                var col = new ColumnExpression(query, new Column(Context.Alias.GetNewColumn()), methodCall.Method.ReturnType);

                query.AddProjection(new AggregateExpression(methodCall.Method, col, query, col.Column.Name));

                return query;
            }

            var parameter = methodCall.Arguments[1].GetParameters().FirstOrDefault();
            var modelExpression = new ModelRefrenceExpressionVisitor(Context, query, parameter)
                .Visit(methodCall.Arguments[1])
                .UnWrap()
                .As<LambdaExpression>()
                .Body;

            Utils.CheckNull(query, "query");

            query.QueryModel = new QueryEntityModel(modelExpression, methodCall.Method.ReturnType, query.QueryModel);

            Context.QueryMapper.MapQuery(parameter, query);
            Context.QueryModelMapper.MapQueryModel(parameter, query.QueryModel);

            query.Projections.Clear();

            var selector = new AggreateExpressionVisitor(Context, query).Visit(modelExpression);
            if (selector.Is<QueryExpression>())
            {
                throw new Exception("Cannot perform an aggregate function on an expression containing an aggregate or a subquery.");
            }

            if (selector.Is<AliasExpression>())
            {
                var alias = selector.As<AliasExpression>();
                var key = new AggregateExpression(methodCall.Method, alias.Expression, query, alias.Alias);

                query.AddProjection(key);
                Context.MemberBindingMapper.Map(modelExpression.As<MemberExpression>(), key);
                Context.ExpressionMapper.Map(modelExpression, key);

                return query.PushDownSubQuery(Context.Alias.GetNewTable());
            }
            else if (selector.Is<ColumnExpression>())
            {
                var col = selector.As<ColumnExpression>();
                var key = new AggregateExpression(methodCall.Method, col, query, Context.Alias.GetNewColumn());

                query.AddProjection(key);
                Context.MemberBindingMapper.Map(modelExpression.As<MemberExpression>(), key);
                Context.ExpressionMapper.Map(modelExpression, key);

                return query.PushDownSubQuery(Context.Alias.GetNewTable());
            }
            else if (selector.NodeType != ExpressionType.Extension)
            {
                var key = new AggregateExpression(methodCall.Method, selector, query, Context.Alias.GetNewColumn());

                query.AddProjection(key);
                Context.MemberBindingMapper.Map(modelExpression.As<MemberExpression>(), key);
                Context.ExpressionMapper.Map(modelExpression, key);
                return query.PushDownSubQuery(Context.Alias.GetNewTable());
            }

            throw new NotImplementedException();
        }
    }
}
