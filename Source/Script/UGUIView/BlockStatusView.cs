﻿using System.Collections.Generic;
using UnityEngine;

namespace PTGame.Blockly.UGUI
{
    public class BlockStatusView : MonoBehaviour
    {
        private InterpreterUpdateStateObserver mObserver;
        private GameObject mStatusObj;
        private Stack<Block> mRunningBlocks;
        private BlockView mRunBlockView;

        private void Awake()
        {
            mRunningBlocks = new Stack<Block>();
            mObserver = new InterpreterUpdateStateObserver(this);
            CSharp.Interpreter.AddObserver(mObserver);

            if (enabled)
                enabled = false;
        }

        private void OnEnable()
        {
            if (mStatusObj == null)
            {
                mStatusObj = GameObject.Instantiate(BlockViewSettings.Get().PrefabStatusLight, BlocklyUI.WorkspaceView.CodingArea, false);
                RectTransform statusRect = mStatusObj.GetComponent<RectTransform>();
                statusRect.anchorMin = statusRect.anchorMax = new Vector2(0, 1);
                statusRect.pivot = 0.5f * Vector2.one;
            }
            mStatusObj.SetActive(true);
        }

        private void OnDisable()
        {
            if (mStatusObj != null)
            {
                mStatusObj.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            CSharp.Interpreter.RemoveObserver(mObserver);
        }

        public void UpdateStatus(InterpreterUpdateState args)
        {
            switch (args.Type)
            {
                case InterpreterUpdateState.RunBlock:
                {
                    mRunningBlocks.Push(args.RunningBlock);
                    mRunBlockView = BlocklyUI.WorkspaceView.GetBlockView(args.RunningBlock);
                    break;
                }
                case InterpreterUpdateState.FinishBlock:
                {
                    if (mRunningBlocks.Count > 0 && mRunningBlocks.Peek() == args.RunningBlock)
                    {
                        mRunningBlocks.Pop();
                        if (mRunningBlocks.Count > 0)
                            mRunBlockView = BlocklyUI.WorkspaceView.GetBlockView(mRunningBlocks.Peek());
                    }
                    break;
                }
                case InterpreterUpdateState.Stop:
                {
                    enabled = false;
                    mRunningBlocks.Clear();
                    mRunBlockView = null;
                    break;
                }
            }
        }

        private void LateUpdate()
        {
            //update the status object on lateupdate, to avoid moving it multiple times in on frame
            if (mRunBlockView != null)
            {
                RectTransform statusRect = mStatusObj.GetComponent<RectTransform>();
                statusRect.SetParent(mRunBlockView.ViewTransform, false);
                statusRect.anchoredPosition = new Vector2(20, -25);
                mRunBlockView = null;
            }
        }

        private class InterpreterUpdateStateObserver : IObserver<InterpreterUpdateState>
        {
            private BlockStatusView mView;

            public InterpreterUpdateStateObserver(BlockStatusView statusView)
            {
                mView = statusView;
            }

            public void OnUpdated(object subject, InterpreterUpdateState args)
            {
                mView.UpdateStatus(args);
            }
        }
    }
}