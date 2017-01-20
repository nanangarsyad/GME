﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Mediation.Interfaces;


namespace Mediation.PlanTools
{
    public class Plan : IPlan
    {
        private Domain domain;
        private Problem problem;
        private List<IOperator> steps;
        private List<CausalLink> dependencies;
        private IState initial;
        private IState goal;

        // Access the plans's domain.
        public Domain Domain
        {
            get { return domain; }
            set { domain = value; }
        }

        // Access the plans's problem.
        public Problem Problem
        {
            get { return problem; }
            set { problem = value; }
        }

        // Access the plans's steps.
        public List<IOperator> Steps
        {
            get { return steps; }
            set { steps = value; }
        }

        // Access the plans's dependencies.
        public List<CausalLink> Dependencies
        {
            get 
            {
                // If the dependencies are not initialized, create them.
                if (dependencies == null)
                    CreateDependencies();

                return dependencies;
            }
            set { dependencies = value; }
        }

        // Access the plan's initial state.
        public IState Initial
        {
            get { return initial; }
            set { initial = value; }
        }

        // Access the plan's goal state.
        public IState Goal
        {
            get { return goal; }
            set { goal = value; }
        }

        // Access the plan's initial step.
        public Operator InitialStep
        {
            get { return new Operator("initial", new List<IPredicate>(), initial.Predicates); }
            set { Initial.Predicates = value.Effects; }
        }

        // Access the plan's goal step.
        public Operator GoalStep
        {
            get { return new Operator("goal", goal.Predicates, new List<IPredicate>()); }
            set { Goal.Predicates = value.Preconditions; }
        }

        public Plan ()
        {
            domain = new Domain();
            problem = new Problem();
            steps = new List<IOperator>();
            dependencies = null;
            initial = new State();
            goal = new State();
        }

        public Plan (Domain domain, Problem problem, List<IOperator> steps)
        {
            this.domain = domain;
            this.problem = problem;
            this.steps = steps;
            this.dependencies = null;
            initial = new State();
            goal = new State();
        }

        public Plan(Domain domain, Problem problem, List<IOperator> steps, IState initial)
        {
            this.domain = domain;
            this.problem = problem;
            this.steps = steps;
            this.dependencies = null;
            this.initial = initial;
            goal = new State();
        }

        // Updates a plan by progressing a single step.
        public Plan GetPlanUpdate (Problem newProblem)
        {
            // Clone the current plan.
            Plan newPlan = (Plan)Clone();

            // Set the input problem as the new problem object.
            newPlan.problem = newProblem;

            // If there are any plan steps remaining.
            if (newPlan.steps.Count > 0)
            {
                // Remove the first step of the old plan.
                newPlan.steps.RemoveAt(0);
            }

            // Remove the current state, if any.
            newPlan.Initial = new State();

            // Create the initial state.
            foreach (Predicate initialPredicate in newProblem.Initial)
                newPlan.Initial.Table.Add(initialPredicate, true);

            // Copy the plan's goal.
            newPlan.GoalStep = (Operator)GoalStep.Clone();

            // Return the new plan.
            return newPlan;
        }

        // Updates a plan by progressing a single step.
        public Plan GetPlanUpdate (Problem newProblem, Operator action)
        {
            // Clone the current plan.
            Plan newPlan = (Plan)Clone();

            // Set the input problem as the new problem object.
            newPlan.problem = newProblem;

            // If there are any plan steps remaining.
            if (newPlan.steps.Count > 0)
            {
                // Remove the taken step.
                newPlan.steps.Remove(action);
            }

            // Remove the current state, if any.
            newPlan.Initial = new State();

            // Create the initial state.
            foreach (Predicate initialPredicate in newProblem.Initial)
                newPlan.Initial.Table.Add(initialPredicate, true);

            // Copy the plan's goal.
            newPlan.GoalStep = (Operator)GoalStep.Clone();

            // Return the new plan.
            return newPlan;
        }

        // Updates a plan by progressing a single step.
        public Plan GetPlanUpdate(Problem newProblem, List<IOperator> actions)
        {
            // Clone the current plan.
            Plan newPlan = (Plan)Clone();

            // Set the input problem as the new problem object.
            newPlan.problem = newProblem;

            // If there are any plan steps remaining.
            if (newPlan.steps.Count > 0)
                // Loop through the taken steps.
                foreach (Operator action in actions)
                    // Remove the taken step.
                    newPlan.steps.Remove(action);

            // Remove the current state, if any.
            newPlan.Initial = new State();

            // Create the initial state.
            foreach (Predicate initialPredicate in newProblem.Initial)
                newPlan.Initial.Table.Add(initialPredicate, true);

            // Copy the plan's goal.
            newPlan.GoalStep = (Operator)GoalStep.Clone();

            // Return the new plan.
            return newPlan;
        }

        // Used for consistent actions.
        public Plan GetPlanUpdateNoStep (Problem newProblem)
        {
            // Clone the current plan.
            Plan newPlan = (Plan)Clone();

            // Set the input problem as the new problem object.
            newPlan.problem = newProblem;

            // Create a step to represent the initial state.
            Operator init = new Operator();

            // Name the initial step.
            init.Name = "initial";

            // Set the step effects to the initial state.
            init.Effects = newProblem.Initial;

            // Add the step to the plan.
            newPlan.InitialStep = init;

            // Copy the plan's goal.
            newPlan.GoalStep = (Operator)GoalStep.Clone();

            // Return the new plan.
            return newPlan;
        }

        // Return the first state of the plan.
        public State GetFirstState ()
        {
            return (State)Initial.Clone();
        }

        // A tree of intermediary world states.
        public List<State> CreateStateTree ()
        {
            // Create the state tree.
            List<State> stateTree = new List<State>();

            // Create the initial state and add it to the tree.
            stateTree.Add(new State(InitialStep.Effects, InitialStep, (Operator)steps.First()));

            // Loop through the plan's steps.
            // Exclude the state before the goal.
            for (int i = 0; i < steps.Count - 1; i++)
            {
                // Create a new state object.
                State state = stateTree.Last().NewState((Operator)steps[i + 1], problem.Objects);

                // Add the new state to the tree.
                stateTree.Add(state);
            }

            return stateTree;
        }

        // Draws dependencies between steps in an input plan.
        private void CreateDependencies ()
        {
            // Reset the causal links.
            dependencies = new List<CausalLink>();

            // Create an empty list of flaws.
            List<Flaw> flaws = new List<Flaw>();

            // Create a list of steps that includes the initial and goal steps.
            List<Operator> allSteps = new List<Operator>();

            // Add the initial step to the new list.
            allSteps.Add(InitialStep);

            // Loop through the plan's steps.
            foreach (Operator step in steps)
            {
                // Add the current step to the new list.
                allSteps.Add(step);

                // Add every precondition to the flaw list.
                foreach (Predicate precon in step.Preconditions)
                    flaws.Add(new Flaw(precon, step));
            }

            // Add the goal step to the new list.
            allSteps.Add(GoalStep);

            // Add every goal precondition to the flaw list.
            foreach (Predicate precon in GoalStep.Preconditions)
                flaws.Add(new Flaw(precon, GoalStep));

            // Resolve every flaw.
            while (flaws.Count > 0)
            {
                bool found = false;

                List<IOperator> span = new List<IOperator>();
                span.Add(flaws.First().step.Clone() as Operator);

                // Loop through the steps:
                // Start at the flaw's step,
                // Loop backward to the initial step.
                for (int i = allSteps.IndexOf(flaws.First().step) - 1; i >= 0; i--)
                {// Loop through each step's effects.
                    foreach (Predicate effect in allSteps[i].Effects)
                    {
                        // Check to see if the effect matches the flaw's predicate.
                        if (flaws.First().precondition.Equals(effect))
                        {
                            // Record that the flaw has been fixed.
                            found = true;

                            // Create a new causal link object.
                            CausalLink link = new CausalLink();

                            // Set the link's predicate to the flaw's precondition.
                            link.Predicate = flaws[0].precondition;

                            // Set the link's head to the flaw's step.
                            link.Head = flaws[0].step;

                            // Set the link's tail to the effect step.
                            link.Tail = allSteps[i];

                            // Set the link's span.
                            link.Span = span;

                            // Add the causal link to the list of links.
                            dependencies.Add(link);

                            // Exit the loop.
                            i = 0;
                        }
                    }

                    span.Add(allSteps.ElementAt(i).Clone() as Operator);
                }

                // If the flaw wasn't fixed, add it to the initial state.
                if (!found)
                    dependencies.Add(new CausalLink(flaws.First().precondition, flaws.First().step, allSteps.First(), span));

                // Remove the current flaw from the list.
                flaws.RemoveAt(0);
            }
        }

        // Displays the contents of the plan.
        public override string ToString ()
        {
            StringBuilder sb = new StringBuilder();

            foreach (Operator step in steps)
                sb.AppendLine(step.ToString());

            return sb.ToString();
        }

        // Displays the contents of the plan.
        public string ToStringDetailed ()
        {
            StringBuilder sb = new StringBuilder();

            foreach (Operator step in steps)
                sb.AppendLine(step.ToString());

            return sb.ToString();
        }

        // Creates a clone of the plan.
        public Object Clone ()
        {
            List<IOperator> newSteps = new List<IOperator>();

            foreach (IOperator step in steps)
                newSteps.Add((IOperator)step.Clone());

            IState newInitial = initial.Clone() as IState;

            return new Plan(domain, problem, newSteps, newInitial);
        }
    }
}