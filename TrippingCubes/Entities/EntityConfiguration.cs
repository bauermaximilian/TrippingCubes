using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace TrippingCubes.Entities
{
    class EntityConfiguration
    {
        private readonly Func<GameWorld, IEntity> entityFactory;

        public Type EntityType { get; }

        public IEnumerable<KeyValuePair<string, string>> EntityParameters
            { get; set; } = Enumerable.Empty<KeyValuePair<string, string>>();

        public EntityConfiguration(Type entityType, 
            Func<GameWorld, IEntity> entityFactory)
        {
            EntityType = entityType ??
                throw new ArgumentNullException(nameof(entityType));
            if (!(typeof(IEntity).IsAssignableFrom(entityType)))
                throw new ArgumentException("The specified type doesn't " +
                    $"implement the {nameof(IEntity)} interface.");

            this.entityFactory = entityFactory ??
                throw new ArgumentNullException(nameof(entityFactory));
        }

        public EntityConfiguration(Type entityType)
            : this(entityType, world => InstantiateEntity(entityType, world))
        {   
        }

        public EntityConfiguration(string entityTypeName)
            : this(ResolveEntityType(entityTypeName))
        {
        }

        private static Type ResolveEntityType(string entityTypeName)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string currentNamespace = 
                typeof(EntityConfiguration).Namespace ?? "";

            Type entityType = executingAssembly.GetType(entityTypeName);

            if (entityType == null)
            {
                entityType = executingAssembly.GetType(
                    $"{currentNamespace}.{entityTypeName}");
            }

            if (entityType == null)
                throw new ArgumentException("The specified entity type name " +
                    "coudln't be resolved into an existing type.");
            else return entityType;
        }

        private static IEntity InstantiateEntity(Type type, GameWorld world)
        {
            ConstructorInfo constructor =
                type.GetConstructor(new Type[] { typeof(GameWorld) });

            if (constructor != null)
            {
                return (IEntity)constructor.Invoke(new object[] { world });
            }

            constructor = type.GetConstructor(new Type[] { });

            if (constructor != null)
            {
                return (IEntity)constructor.Invoke(new object[] { });
            }

            throw new InvalidOperationException("The specified entity " +
                "type doesn't expose a public constructor with either one " +
                $"{nameof(GameWorld)} parameter or no parameters.");
        }

        public IEntity Instantiate(GameWorld gameWorld, 
            IEnumerable<KeyValuePair<string, string>> entityParameters)
        {
            IEntity entity;
            try
            {
                entity = entityFactory(gameWorld);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("The creation of an " +
                    $"instance of type {EntityType.Name} failed.", exc);
            }

            try
            {
                entity.ApplyParameters(EntityParameters);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("The assignment of " +
                    "the parameters from the entity configuration failed.", 
                    exc);
            }

            try
            {
                entity.ApplyParameters(entityParameters);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("The assignment of " +
                    "the parameters from the entity instantiation failed.",
                    exc);
            }

            return entity;
        }
    }
}
