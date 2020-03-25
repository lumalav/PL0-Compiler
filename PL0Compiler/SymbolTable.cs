using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PL0Resources;

namespace PL0Compiler
{
	public class SymbolTable : IEnumerable<NameRecord>
    {
        #region Private Properties
        private readonly Dictionary<string, List<NameRecord>> _items;
        #endregion

        #region Constructor 
        public SymbolTable()
        {
            _items = new Dictionary<string, List<NameRecord>>();
        }
        #endregion

        #region Public Members
        public NameRecord this[string key]
        {

            get => _items.ContainsKey(key) ? _items[key].Last() : null;
            set
            {
                if (Count == Constants.MaxNameTableSize)
                {
                    throw new InvalidOperationException("The symbol table cannot hold any more items.");
                }

                if (_items.ContainsKey(key))
                {
                    if (_items[key].Any(i => i.Equals(value)))
                    {
                        throw new Exception($"The variable {value} was already declared on the level {value.Level}!");
                    }

                    _items[key].Add(value);
                }
                else
                {
                    _items.Add(key, new List<NameRecord> { value });
                }

                Count++;
            }
        }

        public int Count { get; private set; }

        public bool Contains(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException("key");
            return _items.ContainsKey(key);
        }

        public void RemoveAll(Func<NameRecord, bool> func)
        {
            if (func == null) throw new ArgumentNullException("func");

            foreach (var key in _items.Select(i => i.Key).ToList())
            {
                var values = _items[key];

                foreach (var record in values.Where(func).ToList())
                {
                    values.Remove(record);
                    Count--;
                }

                if (!values.Any())
                {
                    _items.Remove(key);
                }
            }
        }

        public void Clear()
        {
            _items.Clear();
            Count = 0;
        }

        public ICollection<string> Keys => _items.Keys; 
        public ICollection<NameRecord> Values => _items.SelectMany(i => i.Value).ToArray(); 
        public IEnumerator<NameRecord> GetEnumerator() => _items.SelectMany(i => i.Value).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}