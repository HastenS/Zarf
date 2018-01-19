﻿using System.Reflection;
using Zarf.Queries.Expressions;
using System.Collections.Generic;

namespace Zarf.Queries
{
    public interface IPropertyNavigationContext
    {
        void AddPropertyNavigation(MemberInfo memberInfo, PropertyNavigation propertyNavigation);

        bool IsPropertyNavigation(MemberInfo memberInfo);

        PropertyNavigation GetNavigation(MemberInfo memberInfo);

        PropertyNavigation GetLastNavigation();
    }
}
