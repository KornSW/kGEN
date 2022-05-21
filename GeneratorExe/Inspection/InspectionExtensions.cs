using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Inspection {

  internal static class InspectionExtensions {

    public static Type Obj2Null(this Type extendee) {
      if(extendee == null || extendee == typeof(Object)) {
        return null;
      }
      return extendee;
    }

    public static bool IsNullableType(this Type extendee) {
      return (extendee.IsGenericType && extendee.GetGenericTypeDefinition() == typeof(Nullable<>));
    }

    public static string GetTypeNameSave(this Type extendee, out bool isNullable) {

      isNullable = extendee.IsNullableType();

      if (isNullable) {
        bool dummy;
        return extendee.GetGenericArguments()[0].GetTypeNameSave(out dummy);
      }
      else {
        return extendee.Name;
      }

    }
    public static bool IsInbound(this ParameterInfo extendee) {
       //return extendee.IsIn || extendee.ParameterType.IsByRef;//|| extendee.IsOptional;
      return !extendee.IsOut;// || extendee.IsOptional;


    }

    public static bool IsOutbound(this ParameterInfo extendee) {
      return extendee.IsOut;
    }

    public static Type[] SortByUsage(this IEnumerable<Type> extendee) {
      var unsortedTypes = new Dictionary<Type, Type[]>();
      var ubound = extendee.Count() - 1;
      foreach (var t in extendee) {
        unsortedTypes.Add(t, t.GetUsedTypes().Where (t=> extendee.Contains(t)).ToArray());
      }

      var sortedTypes = new List<Type>();
      while (unsortedTypes.Any()) {
        var typesLeft = unsortedTypes.Keys.ToArray();
        foreach (var typeLeft in typesLeft) {
          bool hasRelations = unsortedTypes[typeLeft].Where(t=>!sortedTypes.Contains(t)).Any();
          if (!hasRelations) {
            unsortedTypes.Remove(typeLeft);
            sortedTypes.Add(typeLeft);
          }
        }
      }
       
      return sortedTypes.ToArray();
    }

    private static IEnumerable<Type> GetUsedTypes(this Type extendee) {
      var usedTypes = new List<Type>();
      if(extendee.BaseType != null) {
        usedTypes.Add(extendee.BaseType);
      }

      foreach (var prop in extendee.GetProperties()) {
        var pt = prop.PropertyType.GetUnwrappedType();
        if (pt != null) {
          usedTypes.Add(pt);
        }
      }
      foreach (var meth in extendee.GetMethods()) {
        var rt = meth.ReturnType.GetUnwrappedType();
        if (rt != null ) {
          usedTypes.Add(rt);
        }
        foreach (var p in meth.GetParameters()) {
          var pt = p.ParameterType;
          if (pt != null) {
            usedTypes.Add(pt);
          }
        }
      }
      return usedTypes.Distinct();
    }

    private static Type GetUnwrappedType(this Type extendee) {
      if(extendee == null || extendee.FullName == "System.Void") {
        return null;
      }
      if (extendee.IsByRef) {
        extendee = extendee.GetElementType();
      }
      if (extendee.IsArray) {
        extendee = extendee.GetElementType();
      }
      else if (extendee.IsGenericType) {
        var genBase = extendee.GetGenericTypeDefinition();
        var genArg1 = extendee.GetGenericArguments()[0];
        if (typeof(List<>).MakeGenericType(genArg1).IsAssignableFrom(extendee)) {
          extendee = genArg1;
        }
        else if (typeof(Collection<>).MakeGenericType(genArg1).IsAssignableFrom(extendee)) {
          extendee = genArg1;
        }
        if (genBase == typeof(Nullable<>)) {
          extendee = genArg1;
        }
      }
      return extendee;
    }

  }

}