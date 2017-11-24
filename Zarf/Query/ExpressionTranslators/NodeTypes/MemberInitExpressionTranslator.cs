﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zarf.Extensions;

namespace Zarf.Query.ExpressionTranslators.NodeTypes
{
    public class MemberInitExpressionTranslator : Translator<MemberInitExpression>
    {
        public override Expression Translate(IQueryContext context, MemberInitExpression memberInit, IQueryCompiler queryCompiler)
        {
            var newExpression = queryCompiler.Compile(memberInit.NewExpression).As<NewExpression>();
            var bindings = new List<MemberBinding>();

            foreach (var binding in memberInit.Bindings.OfType<MemberAssignment>())
            {
                var bindExpression = queryCompiler.Compile(binding.Expression);
                var memberInfoType = binding.Member.GetPropertyType();

                if (typeof(IEnumerable).IsAssignableFrom(memberInfoType) && memberInfoType != typeof(string))
                {
                    throw new NotImplementedException("not supported!");
                }

                context.EntityMemberMappingProvider.Map(binding.Member, bindExpression);
                bindings.Add(Expression.Bind(binding.Member, bindExpression));
            }

            return memberInit.Update(newExpression, bindings);
        }
    }
}
