using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ClinicManagement.DataAccess.Repositories;
using ClinicManagement.Core;

namespace ClinicManagement.Tests
{
    public class FakeRepository<T> : IRepository<T> where T : class
    {
        public List<T> Cache = new List<T>();
        private int _counter = 1;

        public void Add(T entity)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null && idProperty.PropertyType == typeof(int))
            {
                if ((int)idProperty.GetValue(entity) == 0)
                {
                    idProperty.SetValue(entity, _counter++);
                }
            }
            Cache.Add(entity);
        }

        public IQueryable<T> AsQueryable() => Cache.AsQueryable();

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate) => Cache.AsQueryable().Where(predicate);

        public IEnumerable<T> GetAll() => Cache;

        public T GetById(int id)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null)
            {
                return Cache.FirstOrDefault(e => (int)idProperty.GetValue(e) == id);
            }
            return null;
        }

        public void AddRange(IEnumerable<T> entities)
        {
            foreach (var e in entities) Add(e);
        }

        public void Remove(T entity)
        {
            Cache.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            foreach (var e in entities.ToList()) Remove(e);
        }
    }
}
