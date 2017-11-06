﻿/****************************************************************************
 * Copyright (c) 2017 maoling@putao.com
 * 
 * Helper functions for interpreting C# for blocks.
****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PTGame.Blockly
{
    public partial class CSharpInterpreter : Interpreter
    {
        public override CodeName Name
        {
            get { return CodeName.CSharp; }
        }

        private readonly CoroutineRunner mRunner;
        private readonly Names mVariableNames;
        private readonly Datas mVariableDatas;

        private IEnumerator mRunningProcess;

        public CSharpInterpreter(Names variableNames, Datas variableDatas)
        {
            mVariableNames = variableNames;
            mVariableDatas = variableDatas;
            mRunner = CoroutineRunner.Instance;
        }

        public override void Run(Workspace workspace)
        {
            mVariableNames.Reset();
            mVariableDatas.Reset();

            mRunningProcess = RunWorkspace(workspace);
            mRunner.StartProcess(mRunningProcess);
        }

        public override void Pause()
        {
            if (mRunningProcess != null)
            {
                mRunner.PauseProcess(mRunningProcess);
                CSharp.Interpreter.FireUpdate(new InterpreterUpdateState(InterpreterUpdateState.Pause));
            }
        }

        public override void Resume()
        {
            if (mRunningProcess != null)
            {
                mRunner.ResumeProcess(mRunningProcess);
                CSharp.Interpreter.FireUpdate(new InterpreterUpdateState(InterpreterUpdateState.Resume));
            }
        }

        public override void Stop()
        {
            if (mRunningProcess != null)
            {
                mRunner.StopProcess(mRunningProcess);
                mRunningProcess = null;
                CSharp.Interpreter.FireUpdate(new InterpreterUpdateState(InterpreterUpdateState.Stop));
            }
        }

        /// <summary>
        /// coroutine run code for workspace
        /// todo: execute topblocks in order or synchronously
        /// </summary>
        IEnumerator RunWorkspace(Workspace workspace)
        {
            //traverse all blocks in the workspace and run code for the blocks
            List<Block> blocks = workspace.GetTopBlocks(true);
            foreach (Block block in blocks)
            {
                //exclude the procedure definition blocks
                if (ProcedureDB.IsDefinition(block))
                    continue;
                
                yield return RunBlock(block);
            }
            
            mRunningProcess = null;
            CSharp.Interpreter.FireUpdate(new InterpreterUpdateState(InterpreterUpdateState.Stop));
        }
        
        /// <summary>
        /// run the block in a coroutine way
        /// </summary>
        IEnumerator RunBlock(Block block)
        {
            //check flow 
            if (ControlCmdtor.SkipRunByControlFlow(block))
            {
                yield break;
            }

            if (!block.Disabled)
            {
                CSharp.Interpreter.FireUpdate(new InterpreterUpdateState(InterpreterUpdateState.RunBlock, block));
                yield return GetBlockInterpreter(block).Run(block);
                CSharp.Interpreter.FireUpdate(new InterpreterUpdateState(InterpreterUpdateState.FinishBlock, block));
            }
            
            if (block.NextBlock != null)
                yield return RunBlock(block.NextBlock);
        }
        
        /// <summary>
        /// run code representing the specified value input.
        /// should return a DataStruct
        /// </summary>
        public CustomEnumerator ValueReturn(Block block, string name)
        {
            var targetBlock = block.GetInputTargetBlock(name);
            if (targetBlock == null)
            {
                Debug.Log(string.Format("Value input block of {0} is null", block.Type));
                return new CustomEnumerator(null);
            }
            if (targetBlock.OutputConnection == null)
            {
                Debug.Log(string.Format("Value input block of {0} must have an output connection", block.Type));
                return new CustomEnumerator(null);
            }

            CustomEnumerator etor = new CustomEnumerator(RunBlock(targetBlock));
            etor.Cmdtor = GetBlockInterpreter(targetBlock);
            return etor;
        }

        /// <summary>
        /// run code representing the specified value input. WITH a default DataStruct
        /// </summary>
        public CustomEnumerator ValueReturn(Block block, string name, DataStruct defaultData)
        {
            CustomEnumerator etor = ValueReturn(block, name);
            etor.Cmdtor.DefaultData = defaultData;
            return etor;
        }

        /// <summary>
        /// Run code representing the statement.
        /// </summary>
        public IEnumerator StatementRun(Block block, string name)
        {
            var targetBlock = block.GetInputTargetBlock(name);
            if (targetBlock == null)
            {
                Debug.Log(string.Format("Statement input block of {0} is null", block.Type));
                yield break;
            }
            if (targetBlock.PreviousConnection == null)
            {
                Debug.Log(string.Format("Statement input block of {0} must have a previous connection", block.Type));
                yield break;
            }

            yield return RunBlock(targetBlock);
        }

        public Cmdtor GetBlockInterpreter(Block block)
        {
            Cmdtor cmdtor;
            if (!mCmdMap.TryGetValue(block.Type, out cmdtor))
                throw new Exception(string.Format("Language {0} does not know how to interprete code for block type {1}.", Name, block.Type));
            return cmdtor;
        }
    }
}