﻿using AssetRipper.Core.Classes.Shader;
using AssetRipper.Core.Classes.Shader.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShaderLabConvert
{
    public class USILCBufferMetadder : IUSILOptimizer
    {
        private UShaderProgram _shader;
        private ShaderSubProgram _shaderData;

        public bool Run(UShaderProgram shader, ShaderSubProgram shaderData)
        {
            _shader = shader;
            _shaderData = shaderData;

            List<USILInstruction> instructions = shader.instructions;
            foreach (USILInstruction instruction in instructions)
            {
                if (instruction.destOperand != null)
                {
                    UseMetadata(instruction.destOperand);
                }

                foreach (USILOperand operand in instruction.srcOperands)
                {
                    UseMetadata(operand);
                }
            }
            return true; // any changes made?
        }

        private void UseMetadata(USILOperand operand)
        {
            if (operand.operandType == USILOperandType.ConstantBuffer)
            {
                int cbRegIdx = operand.registerIndex;
                int cbArrIdx = operand.arrayIndex;

                List<int> operandMaskAddresses = new List<int>();
                foreach (int operandMask in operand.mask)
                {
                    operandMaskAddresses.Add(cbArrIdx * 16 + operandMask * 4);
                }

                HashSet<NumericShaderParameter> cbParams = new HashSet<NumericShaderParameter>();
                List<int> cbMasks = new List<int>();

				BufferBinding binding = _shaderData.ConstantBufferBindings.First(b => b.Index == cbRegIdx);
				ConstantBuffer constantBuffer = _shaderData.ConstantBuffers.First(b => b.Name == binding.Name);

				// Search children fields
				foreach (NumericShaderParameter param in constantBuffer.AllNumericParams)
                {
                    int paramCbStart = param.Index;
                    int paramCbSize = param.RowCount * param.ColumnCount * 4;
                    int paramCbEnd = paramCbStart + paramCbSize;

                    foreach (int operandMaskAddress in operandMaskAddresses)
                    {
                        if (operandMaskAddress >= paramCbStart && operandMaskAddress < paramCbEnd)
                        {
                            cbParams.Add(param);
                            
                            int maskIndex = (operandMaskAddress - paramCbStart) / 4;
                            if (param.IsMatrix)
                            {
                                maskIndex %= 4;
                            }
                            cbMasks.Add(maskIndex);
                        }
                    }
                }

                // Search children structs and its fields
                foreach (StructParameter stParam in constantBuffer.StructParams)
				{
					foreach (NumericShaderParameter cbParam in stParam.AllNumericMembers)
                    {
                        int paramCbStart = cbParam.Index;
                        int paramCbSize = cbParam.RowCount * cbParam.ColumnCount * 4;
                        int paramCbEnd = paramCbStart + paramCbSize;

                        foreach (int operandMaskAddress in operandMaskAddresses)
                        {
                            if (operandMaskAddress >= paramCbStart && operandMaskAddress < paramCbEnd)
                            {
                                cbParams.Add(cbParam);

                                int maskIndex = (operandMaskAddress - paramCbStart) / 4;
                                if (cbParam.IsMatrix)
                                {
                                    maskIndex %= 4;
                                }
                                cbMasks.Add(maskIndex);
                            }
                        }
                    }
                }

                // Multiple params got opto'd into one operation
                if (cbParams.Count > 1)
                {
                    operand.operandType = USILOperandType.Multiple;
                    operand.children = new USILOperand[cbParams.Count];

                    int i = 0;
                    List<string> paramStrs = new List<string>();
                    foreach (NumericShaderParameter param in cbParams)
                    {
                        USILOperand childOperand = new USILOperand();
                        childOperand.operandType = USILOperandType.ConstantBuffer;

                        childOperand.mask = MatchMaskToConstantBuffer(operand.mask, param.Index, param.RowCount);
                        childOperand.metadataName = param.Name;
                        childOperand.metadataNameAssigned = true;

                        operand.children[i++] = childOperand;
                    }
                }
                else if (cbParams.Count == 1)
                {
					NumericShaderParameter param = cbParams.First();
                    string body = param.Name;

                    // Matrix
                    if (param.IsMatrix)
                    {
                        int matrixIdx = cbArrIdx - param.Index / 16;
                        body = $"{body}";

                        operand.operandType = USILOperandType.Matrix;
                        operand.arrayIndex = matrixIdx;
                        operand.transposeMatrix = true;
                    }
                    operand.mask = cbMasks.ToArray();
                    operand.metadataName = body;
                    operand.metadataNameAssigned = true;

                    if (cbMasks.Count == param.ColumnCount && !param.IsMatrix)
                    {
                        operand.displayMask = false;
                    }
                }
            }
        }

        private int[] MatchMaskToConstantBuffer(int[] mask, int pos, int size)
        {
            // Mask is aligned (x, xy, xyz, xyzw)
            // todo: bad opto breaks things lol keep this out
            // if (pos % 16 == 0)
            // {
            //     return mask;
            // }

            int offset = (pos / 4) % 4;
            List<int> result = new List<int>();
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] >= offset && mask[i] < offset + size)
                    result.Add(mask[i] - offset);
            }
            return result.ToArray();
        }
    }
}