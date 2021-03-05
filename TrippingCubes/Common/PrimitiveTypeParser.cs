using ShamanTK.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TrippingCubes.Common
{
    static class PrimitiveTypeParser
    {
        private static readonly Regex invariantCultureFloatNumber =
            new Regex(@"[+-]?([0-9]*[.])?[0-9]+");

        private static readonly Dictionary<Type, Func<string, object>> 
            typeConverters = new Dictionary<Type, Func<string, object>>();

        static PrimitiveTypeParser()
        {
            typeConverters.Add(typeof(string), value => value);
            typeConverters.Add(typeof(ResourcePath), ParseResourcePath);
            typeConverters.Add(typeof(bool), ParseBool);
            typeConverters.Add(typeof(int), ParseInt);
            typeConverters.Add(typeof(float), ParseFloat);
            typeConverters.Add(typeof(double), ParseDouble);
            typeConverters.Add(typeof(Vector2), ParseVector2);
            typeConverters.Add(typeof(Vector3), ParseVector3);
            typeConverters.Add(typeof(Quaternion), ParseQuaternion);
        }

        public static void TryAssign(
            IEnumerable<KeyValuePair<string, string>> propertyAssignments,
            object targetObject, bool throwOnError)
        {
            Type targetObjectType = targetObject.GetType();

            foreach (var propertyAssignment in propertyAssignments)
            {
                PropertyInfo property = targetObjectType.GetProperty(
                    propertyAssignment.Key, 
                    BindingFlags.Instance | BindingFlags.Public | 
                    BindingFlags.SetProperty);

                if (property != null)
                {
                    Type propertyType = property.PropertyType;

                    if (TryParse(propertyAssignment.Value, propertyType,
                        out object value))
                    {
                        if (propertyType.IsAssignableFrom(value.GetType()))
                        {
                            try { property.SetValue(targetObject, value); }
                            catch (Exception exc)
                            {
                                if (throwOnError)
                                {
                                    throw new InvalidOperationException(
                                        "The property " +
                                        $"{propertyAssignment.Key} couldn't " +
                                        "be assigned with the parsed value.",
                                        exc);
                                }
                            }
                        }
                    }
                    else if (throwOnError)
                    {
                        throw new InvalidOperationException("The parameter " +
                            $"value of {propertyAssignment.Key} couldn't be " +
                            "parsed into a value of the type " +
                            $"{propertyType.Name} of the target property.");
                    }
                }
                else if (throwOnError)
                {
                    throw new InvalidOperationException("The parameter " +
                        $"{propertyAssignment.Key} couldn't be matched with " +
                        "a public (writeable) property of the target object.");
                }
            }
        }

        public static bool TryParse<TargetT>(string valueString, 
            out TargetT value)
        {
            if (TryParse(valueString, typeof(TargetT), out object objValue)
                && objValue is TargetT typedValue)
            {
                value = typedValue;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public static bool TryParse(string valueString, Type targetType,
            out object value)
        {
            value = null;
            if (valueString != null && typeConverters.ContainsKey(targetType))
            {
                try
                {
                    value = typeConverters[targetType](valueString);
                    return value != null;
                }
                catch
                {
                    return false;
                }
            }
            else return false;
        }

        #region Internally used converter methods
        private static object ParseResourcePath(string valueString)
        {
            try { return new ResourcePath(valueString); }
            catch { return null; }
        }

        private static object ParseInt(string valueString)
        {
            if (int.TryParse(valueString, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int value)) return value;
            else return null;
        }

        private static object ParseBool(string valueString)
        {
            if (bool.TryParse(valueString, out bool value)) return value;
            else return null;
        }

        private static object ParseFloat(string valueString)
        {
            if (float.TryParse(valueString, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float value)) return value;
            else return null;
        }

        private static object ParseDouble(string valueString)
        {
            if (double.TryParse(valueString, NumberStyles.Float,
                CultureInfo.InvariantCulture, out double value)) return value;
            else return null;
        }

        private static object ParseVector2(string valueString)
        {
            List<float> numbers = ParseFloats(valueString).ToList();

            if (numbers.Count >= 2)
                return new Vector2(numbers[0], numbers[1]);
            else return null;
        }

        private static object ParseVector3(string valueString)
        {
            List<float> numbers = ParseFloats(valueString).ToList();

            if (numbers.Count >= 3)
                return new Vector3(numbers[0], numbers[1], numbers[2]);
            else return null;
        }

        private static object ParseQuaternion(string valueString)
        {
            List<float> numbers = ParseFloats(valueString).ToList();

            if (numbers.Count >= 4)
                return new Quaternion(
                    numbers[0], numbers[1], numbers[2], numbers[3]);
            else return null;
        }
        #endregion

        #region Internally used methods
        private static IEnumerable<double> ParseDoubles(string valueString)
        {
            Match match = invariantCultureFloatNumber.Match(valueString);
            while (match.Success)
            {
                if (double.TryParse(match.Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double value))
                    yield return value;

                match = match.NextMatch();
            }
        }

        public static IEnumerable<float> ParseFloats(string valueString)
        {
            foreach (double number in ParseDoubles(valueString))
                yield return (float)number;
        }
        #endregion
    }
}
