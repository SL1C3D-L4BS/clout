using System;
using System.Collections.Generic;

namespace Clout.Core
{
    /// <summary>
    /// A single state in the state machine. Contains lists of actions
    /// that execute each frame, plus callbacks for enter/exit.
    ///
    /// Strategy pattern: each StateAction is a pluggable behavior.
    /// </summary>
    public class State
    {
        public string id;

        public List<StateAction> updateActions = new List<StateAction>();
        public List<StateAction> fixedUpdateActions = new List<StateAction>();
        public List<StateAction> lateUpdateActions = new List<StateAction>();

        public Action onEnter;
        public Action onExit;
    }
}
