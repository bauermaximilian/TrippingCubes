using System;
using System.Collections.Generic;
using System.Text;
using ShamanTK;
using ShamanTK.Controls;

namespace GameCraft
{
    public class ControlScheme<ElementEnumT> where ElementEnumT : Enum
    {
        private readonly Dictionary<ElementEnumT, ControlMapping> mappings = 
            new Dictionary<ElementEnumT, ControlMapping>();

        /// <summary>
        /// Gets a collection of elements which have been assigned to a
        /// <see cref="ControlMapping"/>.
        /// </summary>
        public ICollection<ElementEnumT> DefinedElements => mappings.Keys;

        /// <summary>
        /// Gets or sets the <see cref="ControlMapping"/> assigned to a
        /// specified <paramref name="element"/>.
        /// </summary>
        /// <param name="element">
        /// The control scheme element the attached 
        /// <see cref="ControlMapping"/> mapping should be retrieved,
        /// added or cleared (by assinging <c>null</c> or 
        /// <see cref="ControlMapping.Empty"/>).
        /// </param>
        /// <returns>
        /// The associated <see cref="ControlMapping"/> instance or
        /// <see cref="ControlMapping.Empty"/>, if the specified
        /// <paramref name="element"/> is not assigned.
        /// </returns>
        /// <remarks></remarks>
        public ControlMapping this[ElementEnumT element]
        {
            get
            {
                if (mappings.TryGetValue(element, out ControlMapping value))
                    return value;
                else return ControlMapping.Empty;
            }
            set
            {
                if (value != null && value != ControlMapping.Empty) 
                    mappings[element] = value;
                else mappings.Remove(element);
            }
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="ControlScheme{ElementEnumT}"/> class.
        /// </summary>
        public ControlScheme() { }
    }

    class Test
    {
        enum TestEnum { Up, Down, Left, Right }

        void TestMethod()
        {
            ControlScheme<TestEnum> scheme1 = new ControlScheme<TestEnum>();
            float value = scheme1[TestEnum.Down].Value;// No KeyNotFound/NullPointer!
        }
    }
}
