using BuilderAPIData;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace BuilderAPI
{
    internal class TriggerPathSearcher
    {
        private class SearchNode
        {
            public string PathPart { get; set; } = "";
            public HashSet<TriggerRecord> TriggerAt { get; init; } = new();
            public Dictionary<string, SearchNode> ChildNodes { get; init; } = new ();
        };

        private Dictionary<string, SearchNode> _pathSearch = new ();

        public void AddTrigger(TriggerRecord trigger)
        {
            var currentPath = trigger.BasePath.Trim(Path.PathSeparator);
            var currentLevel = _pathSearch;
            SearchNode? currentNode = FindAndSpitNode(_pathSearch, currentPath);
            if(currentNode == null)
            {
                var newNode = new SearchNode();
                newNode.TriggerAt.Add(trigger);
                _pathSearch[currentPath] = newNode;
                return;
            }
            while (currentNode != null)
            {
                currentPath = currentPath.Substring(currentNode.PathPart.Length).Trim(Path.PathSeparator);
                if (string.IsNullOrEmpty(currentPath)) break;

                currentLevel = currentNode.ChildNodes;
                var tempNode = FindAndSpitNode(currentLevel, currentPath);

                if (tempNode == null) break;
                currentNode = tempNode;
            }

            if (currentNode != null && !string.IsNullOrEmpty(currentPath))
            {
                var newNode = new SearchNode();
                newNode.TriggerAt.Add(trigger);
                currentNode.ChildNodes[currentPath] = newNode;
            }
            else if (currentNode != null && string.IsNullOrEmpty(currentPath))
            {
                currentNode.TriggerAt.Add(trigger);
            }
        }

        public HashSet<TriggerRecord> GetTriggers(string path, List<string> fileChangeTags)
        {
            var tempPath = path.Trim(Path.PathSeparator);
            var currentMap = _pathSearch;
            var collectedTriggers = new HashSet<TriggerRecord>();
            SearchNode? currentNode = null;
            do
            {
                currentNode = Find(currentMap, tempPath);
                if(currentNode != null)
                {
                    currentNode.TriggerAt
                        .Where(t => t.FileTags.Intersect(fileChangeTags).Any())
                        .ToList()
                        .ForEach(i => collectedTriggers.Add(i));
                    currentMap = currentNode.ChildNodes;
                    tempPath = tempPath.Substring(currentNode.PathPart.Length).Trim(Path.PathSeparator);
                }
            } while (currentNode != null && !string.IsNullOrEmpty(tempPath));

            return collectedTriggers;
        }

        public void RemoveTrigger(TriggerRecord trigger)
        {
            var triggers = GetTriggers(trigger.BasePath, trigger.FileTags);
            if (triggers != null)
            {
                triggers.Remove(trigger);
            }
        }

        private SearchNode? Find(Dictionary<string, SearchNode> pathMap, string currentPath)
        {
            var keys = pathMap.Keys.ToList();
            var currPath = currentPath.Trim(Path.PathSeparator);
            foreach (var key in keys)
            {
                if (currPath.StartsWith(key)) return pathMap[key];
            }
            return null;
        }

        private SearchNode? FindAndSpitNode(Dictionary<string, SearchNode> pathMap, string currentPath)
        {
            var keys = pathMap.Keys.ToList();
            foreach ( var key in keys )
            {
                if (currentPath.StartsWith(key))
                { 
                    return pathMap[key];
                }
                if (key.StartsWith(currentPath))
                {
                    var tempNode = pathMap[key];
                    pathMap.Remove(key);
                    var newKeyNode = key.Substring(currentPath.Length).Trim(Path.PathSeparator);
                    var newNode = new SearchNode() { PathPart = newKeyNode };
                    tempNode.PathPart = currentPath;
                    pathMap[currentPath] = tempNode;
                    tempNode.ChildNodes[newKeyNode] = newNode;
                    return tempNode;
                }
            }

            return null;
        }
    }
}
