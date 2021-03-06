﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Zarf.Extensions;
using Zarf.Generators.Functions.Providers;
using Zarf.Metadata.Entities;
using Zarf.Query.Expressions;
using Zarf.Update.Expressions;

namespace Zarf.Generators
{
    public abstract partial class SQLGenerator : ExpressionVisitor, ISQLGenerator
    {
        /// <summary>
        /// 生成SQL的跟表达式
        /// </summary>
        protected Expression Root { get; set; }

        /// <summary>
        /// 参数计数
        /// </summary>
        private int _parameterCounter = 0;

        private List<DbParameter> _parameters;

        /// <summary>
        /// 生成的参数
        /// </summary>
        protected List<DbParameter> Parameters => _parameters;

        /// <summary>
        /// SQL
        /// </summary>
        protected StringBuilder SQL { get; set; }

        protected ISQLFunctionHandlerProvider SQLFunctionHandlerProvider { get; }

        public SQLGenerator(ISQLFunctionHandlerProvider sqlFunctionHandlerProvider)
        {
            SQLFunctionHandlerProvider = sqlFunctionHandlerProvider;
        }

        public virtual string Generate(Expression expression, List<DbParameter> parameters)
        {
            lock (this)
            {
                _parameterCounter = 0;
                _parameters = parameters;

                SQL = new StringBuilder();

                Attach(expression);

                return SQL.ToString();
            }
        }

        /// <summary>
        /// 将Expression生成SQL,并附加到当前SQL
        /// </summary>
        /// <param name="exp"></param>
        public virtual void Attach(Expression expression)
        {
            if (expression != null)
            {
                Visit(expression);
            }
        }

        public override Expression Visit(Expression node)
        {
            if (Root == null)
            {
                Root = node;
            }

            return base.Visit(node);
        }

        /// <summary>
        /// 附加SQL到当前SQL中
        /// </summary>
        /// <param name="text"></param>
        public virtual void Attach(string text)
        {
            SQL.Append(text);
        }

        protected DbParameter CreateParameter(object parameterValue)
        {
            return new DbParameter("@P" + _parameterCounter++, parameterValue);
        }

        protected virtual Expression VisitColumn(ColumnExpression column)
        {
            if (column.Select != null && !column.Select.Alias.IsNullOrEmpty())
            {
                SQL.Append(column.Select.Alias.Escape());
                SQL.Append('.');
            }

            if (column.Column == null)
            {
                Attach(" NULL ");
            }
            else
            {
                SQL.Append(column.Column.Name.Escape());
            }

            if (!column.Alias.IsNullOrEmpty())
            {
                Attach(" AS ");
                SQL.Append(column.Alias.Escape());
            }

            return column;
        }

        protected virtual Expression VisitAlias(AliasExpression alias)
        {
            if (alias.Expression is SelectExpression)
            {
                Attach("( ");
                Attach(alias.Expression);
                Attach(" ) ");
            }
            else
            {
                Attach(alias.Expression);
            }

            Attach(" AS ");
            Attach(alias.Alias);
            return alias;
        }

        protected virtual Expression VisitExcept(ExceptExpression except)
        {
            Attach(" Except ");
            Attach(except.Select);
            return except;
        }

        protected virtual Expression VisitGroup(GroupExpression group)
        {
            BuildColumns(group.Columns);
            return group;
        }

        protected virtual Expression VisitIntersect(IntersectExpression intersec)
        {
            Attach(" INTERSECT ");
            Attach(intersec.Select);
            return intersec;
        }

        protected virtual Expression VisitUnion(UnionExpression union)
        {
            Attach(" UNION ");
            Attach(union.IncludeRepated ? "  ALL  " : string.Empty);
            Attach(union.Select);
            return union;
        }

        protected virtual Expression VisitJoin(JoinExpression join)
        {
            switch (join.JoinType)
            {
                case JoinType.Left:
                    Attach(" Left JOIN ");
                    break;
                case JoinType.Right:
                    Attach(" Right JOIN ");
                    break;
                case JoinType.Full:
                    Attach(" Full JOIN ");
                    break;
                case JoinType.Inner:
                    Attach(" Inner JOIN ");
                    break;
                case JoinType.Cross:
                    Attach(" Cross JOIN ");
                    break;
            }

            if (join.Select.CanJoinAsFlatTable() && join.Select.Projections.Count == 0)
            {
                BuildFromTable(join.Select);
            }
            else
            {
                Attach(join.Select);
                Attach("  AS " + join.Select.Alias.Escape());
            }

            if (join.JoinType != JoinType.Cross)
            {
                Attach(" ON ");
                Attach(join.Predicate ?? Utils.ExpressionTrue);
            }

            return join;
        }

        protected virtual Expression VisitOrder(OrderExpression order)
        {
            var direction = order.Direction == OrderDirection.Desc
                ? " DESC "
                : " ASC ";

            BuildColumns(order.Columns);
            SQL.Append(direction);
            return order;
        }

        protected virtual Expression VisitWhere(WhereExperssion where)
        {
            Attach(" WHERE ");
            Attach(where.Predicate);
            return where;
        }

        protected override Expression VisitConstant(ConstantExpression constant)
        {
            if (constant.Type.IsPrimtiveType())
            {
                var parameter = CreateParameter(constant.Value);
                Attach(parameter.Name);
                Parameters.Add(parameter);
            }
            else
            {
                var parameter = CreateParameter(constant.Value.ToString());
                Attach(parameter.Name);
                Parameters.Add(parameter);
            }

            return constant;
        }

        protected override Expression VisitUnary(UnaryExpression unary)
        {
            switch (unary.NodeType)
            {
                case ExpressionType.Not:
                    Attach(" NOT ( ");
                    if (unary.Operand.Is<ConstantExpression>() && unary.Operand.Type == typeof(bool))
                    {
                        Attach(" 1 =");
                    }

                    Attach(unary.Operand);
                    Attach(" )");
                    break;
                default:
                    Visit(unary.Operand);
                    break;
            }

            return unary;
        }

        protected override Expression VisitBinary(BinaryExpression binary)
        {
            if (!Utils.OperatorMap.TryGetValue(binary.NodeType, out string op))
            {
                throw new NotImplementedException("ViistBinary");
            }

            var leftIsNull = binary.Left.IsNullValueConstant();
            var righIsNull = binary.Right.IsNullValueConstant();

            var left = binary.Left;
            var right = binary.Right;

            if (leftIsNull && righIsNull)
            {
                Attach(Expression.Constant(1));
                SQL.Append(op);
                Attach(Expression.Constant(1));

                return binary;
            }

            if (leftIsNull || righIsNull)
            {
                Attach(leftIsNull ? right : left);
                if (binary.NodeType == ExpressionType.Equal)
                {
                    Attach(" IS NULL ");
                    return binary;
                }

                if (binary.NodeType == ExpressionType.NotEqual)
                {
                    Attach(" IS NOT NULL ");
                    return binary;
                }

                throw new ArgumentNullException(" NULL ");
            }

            Attach(left);
            SQL.Append(op);
            Attach(right);

            return binary;

        }

        protected virtual void BuildColumns(IEnumerable<ColumnExpression> columns)
        {
            foreach (var column in columns)
            {
                VisitColumn(column);
                Attach(",");
            }

            SQL.Length--;
        }

        protected virtual void BuildLimit(SelectExpression select)
        {
            if (select.Limit != 0)
            {
                Attach(" LIMIT  ");
                Attach(select.Limit.ToString());
                Attach(" ");
                return;
            }

            if (select.Offset != null)
            {
                Attach(" LIMIT  ");
                Attach(int.MaxValue.ToString());
                Attach(" ");
            }
        }

        protected virtual void BuildDistinct(SelectExpression select)
        {
            if (select.IsDistinct)
            {
                Attach(" DISTINCT ");
            }
        }

        protected virtual void BuildProjections(SelectExpression select)
        {
            if (select.Projections == null || select.Projections.Count == 0)
            {
                Attach("*");
            }
            else
            {
                foreach (var item in select.Projections)
                {
                    Attach(item);
                    Attach(",");
                }

                SQL.Length--;
            }
        }

        protected virtual void BuildSubQuery(SelectExpression select)
        {
            if (select.ChildSelect != null)
            {
                Attach(select.ChildSelect);
            }
        }

        protected virtual void BuildFromTable(SelectExpression select)
        {
            if (select.ChildSelect == null)
            {
                Utils.CheckNull(select.Table, "query.Table is null");
                Attach(select.Table.Schema.Escape());
                Attach(".");
                Attach(select.Table.Name.Escape());
            }

            if (!select.Alias.IsNullOrEmpty())
            {
                Attach(" AS ");
                Attach(select.Alias.Escape());
            }
        }

        protected virtual void BuildJoins(SelectExpression select)
        {
            if (select.Joins == null || select.Joins.Count == 0)
            {
                return;
            }

            foreach (var join in select.Joins)
            {
                Attach(join);
            }
        }

        protected virtual void BuildSets(SelectExpression select)
        {
            if (select.Sets == null || select.Sets.Count == 0)
            {
                return;
            }

            foreach (var set in select.Sets)
            {
                Attach(set);
            }
        }

        protected virtual void BuildOrders(SelectExpression select)
        {
            if (select.Orders == null || select.Orders.Count == 0)
            {
                return;
            }

            Attach(" ORDER BY ");

            foreach (var order in select.Orders)
            {
                Attach(order);
                Attach(",");
            }

            SQL.Length--;
        }

        protected virtual void BuildGroups(SelectExpression select)
        {
            if (select.Groups == null || select.Groups.Count == 0)
            {
                return;
            }

            Attach(" GROUP BY ");

            foreach (var group in select.Groups)
            {
                Attach(group);
                Attach(",");
            }

            SQL.Length--;
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCall)
        {
            var handlers = SQLFunctionHandlerProvider.GetHandlers(methodCall.Method);
            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    if (handler.HandleFunction(this, methodCall))
                    {
                        return methodCall;
                    }
                }
            }

            throw new NotSupportedException($"the method {methodCall.Method.Name} of type {methodCall.Method.DeclaringType} is not supported!");
        }

        protected override Expression VisitExtension(Expression node)
        {
            switch (node)
            {
                case SelectExpression select:
                    VisitQuery(select);
                    break;
                case WhereExperssion where:
                    VisitWhere(where);
                    break;
                case ColumnExpression column:
                    VisitColumn(column);
                    break;
                case JoinExpression join:
                    VisitJoin(join);
                    break;
                case OrderExpression order:
                    VisitOrder(order);
                    break;
                case GroupExpression group:
                    VisitGroup(group);
                    break;
                case UnionExpression union:
                    VisitUnion(union);
                    break;
                case ExceptExpression except:
                    VisitExcept(except);
                    break;
                case IntersectExpression intersect:
                    VisitIntersect(intersect);
                    break;
                case AggregateExpression aggregate:
                    VisitAggregate(aggregate);
                    break;
                case SkipExpression skip:
                    VisitSkip(skip);
                    break;
                case AllExpression all:
                    VisitAll(all);
                    break;
                case AnyExpression any:
                    VisitAny(any);
                    break;
                case ExistsExpression exists:
                    VisitExists(exists);
                    break;
                case DbStoreExpression store:
                    VisitStore(store);
                    break;
                case AliasExpression alias:
                    VisitAlias(alias);
                    break;
                case CaseWhenExpression caseWhen:
                    VisitCaseWhen(caseWhen);
                    break;
            }

            return node;
        }

        protected virtual Expression VisitCaseWhen(CaseWhenExpression caseWhen)
        {
            Attach(" ( CASE WHEN  ");
            Attach(caseWhen.CaseWhen);
            Attach(" THEN ");
            Attach(caseWhen.Then);
            Attach(" ELSE ");
            Attach(caseWhen.Else);
            Attach(" END ) ");

            return caseWhen;
        }

        protected virtual Expression VisitAll(AllExpression all)
        {
            if (ReferenceEquals(all, Root))
            {
                Attach("SELECT");
            }

            Attach(" (SELECT CASE WHEN ");
            Attach(" NOT EXISTS( ");
            Attach(all.Select);
            Attach(" ) THEN   CAST(1 AS BIT) ELSE CAST(0 AS BIT) END )");
            return all;
        }

        protected virtual Expression VisitAny(AnyExpression any)
        {
            if (ReferenceEquals(any, Root))
            {
                Attach("SELECT");
            }

            Attach(" (SELECT CASE WHEN ");
            Attach(" EXISTS( ");
            Attach(any.Select);
            Attach(" ) THEN   CAST(1 AS BIT) ELSE CAST(0 AS BIT) END )");
            return any;
        }

        protected virtual Expression VisitAggregate(AggregateExpression exp)
        {
            switch (exp.Method.Name)
            {
                case "Min":
                    Attach("MIN");
                    break;
                case "Max":
                    Attach("Max");
                    break;
                case "Sum":
                    Attach("Sum");
                    break;
                case "Average":
                    Attach("CAST( AVG");
                    break;
                case "Count":
                    Attach("Count");
                    break;
                case "LongCount":
                    Attach("Count_Big");
                    break;
                default:
                    throw new NotImplementedException($"method {exp.Method.Name} is not supported!");
            }

            Attach(" (");

            if (exp.KeySelector == null)
            {
                Attach("1");
            }
            else
            {
                Attach(exp.KeySelector);
            }

            Attach(" ) ");

            if (exp.Method.Name == "Average")
            {
                if (ReflectionUtil.DecimalNullableType == exp.Method.ReturnType ||
                    ReflectionUtil.DecimalType == exp.Method.ReturnType)
                {
                    Attach(" As NUMERIC(38,18) ) ");
                }
                else if (ReflectionUtil.FloatNullableType == exp.Method.ReturnType ||
                    ReflectionUtil.FloatType == exp.Method.ReturnType)
                {
                    Attach(" As Real ) ");
                }
                else
                {
                    Attach(" As Float ) ");
                }
            }

            Attach(!exp.Alias.IsNullOrEmpty() ? " AS " + exp.Alias : string.Empty);
            return exp;
        }

        protected abstract Expression VisitQuery(SelectExpression select);

        protected abstract Expression VisitSkip(SkipExpression skip);

        protected abstract Expression VisitExists(ExistsExpression exists);

        protected abstract Expression VisitStore(DbStoreExpression store);
    }
}
