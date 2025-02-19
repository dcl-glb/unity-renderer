using System.Collections.Generic;
using DCL;
using NUnit.Framework;

namespace Tests
{
    public class CRDTTests
    {
        [Test]
        public void MessagesProcessedCorrectly()
        {
            string[] filesPath = CRDTTestsUtils.GetTestFilesPath();

            for (int i = 0; i < filesPath.Length; i++)
            {
                ParsedCRDTTestFile parsedFile = CRDTTestsUtils.ParseTestFile(filesPath[i]);
                AssertTestFile(parsedFile);
            }
        }

        private void AssertTestFile(ParsedCRDTTestFile parsedFile)
        {
            CRDTProtocol crdt = new CRDTProtocol();
            for (int i = 0; i < parsedFile.fileInstructions.Count; i++)
            {
                ParsedCRDTTestFile.TestFileInstruction instruction = parsedFile.fileInstructions[i];
                if (instruction.instructionType == ParsedCRDTTestFile.InstructionType.MESSAGE)
                {
                    CRDTMessage msg = ParsedCRDTTestFile.InstructionToMessage(instruction);
                    crdt.ProcessMessage(msg);
                }
                else if (instruction.instructionType == ParsedCRDTTestFile.InstructionType.FINAL_STATE)
                {
                    var finalState = ParsedCRDTTestFile.InstructionToFinalState(instruction);

                    Assert.IsTrue(AreStatesEqual(crdt, finalState), $"Final state mismatch {instruction.testSpect} " +
                                                                    $"in line:{instruction.lineNumber} for file {instruction.fileName}");
                    crdt = new CRDTProtocol();
                }
            }
        }

        static bool AreStatesEqual(CRDTProtocol crdt, Dictionary<string, CRDTMessage> inputDictionary)
        {
            if (inputDictionary.Count != crdt.state.Count)
            {
                return false;
            }

            foreach (var kvp in inputDictionary)
            {
                if (!crdt.state.TryGetValue(kvp.Key, out CRDTMessage storedMessage))
                    return false;

                if (!(storedMessage.timestamp == kvp.Value.timestamp
                      && CRDTProtocol.IsSameData(storedMessage.data, kvp.Value.data)))
                    return false;
            }

            return true;
        }
    }
}