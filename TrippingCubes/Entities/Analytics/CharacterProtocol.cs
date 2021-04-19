using ShamanTK.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace TrippingCubes.Entities.Analytics
{
    class CharacterProtocol
    {
        public ICharacter Character { get; }

        public SortedList<TimeSpan, int> HealthPoints { get; }
            = new SortedList<TimeSpan, int>();

        public SortedList<TimeSpan, string> States { get; }
            = new SortedList<TimeSpan, string>();

        private TimeSpan Delta => DateTime.Now - protocolStart;

        private readonly DateTime protocolStart = DateTime.MinValue;

        public CharacterProtocol(ICharacter character)
        {
            Character = character;

            protocolStart = DateTime.Now;
            Character_HealthPointsChanged(Character.HealthPoints,
                Character.HealthPoints);
            Character_StateChanged(Character.CurrentState, 
                Character.CurrentState);

            Character.HealthPointsChanged += Character_HealthPointsChanged;
            Character.StateChanged += Character_StateChanged;
        }

        private void Character_StateChanged(string previousValue, 
            string currentValue)
        {
            States.Add(Delta, currentValue);
        }

        private void Character_HealthPointsChanged(int previousValue, 
            int currentValue)
        {
            HealthPoints.Add(Delta, currentValue);
        }

        public void Save(IFileSystem fileSystem, 
            FileSystemPath protocolFileDirectory)
        {
            try
            {
                using Stream healthPointsProtocolStream =
                    OpenUniqueProtocolFileStream(protocolFileDirectory,
                        Character.Name ?? "UnnamedCharacter", 
                        "healthpoints", fileSystem);
                SaveProtocolToCsv(HealthPoints,
                    v => v.ToString(CultureInfo.InvariantCulture),
                    healthPointsProtocolStream, "time", "health");
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("Saving the protocol " +
                    "of health points failed.", exc);
            }

            try
            {
                using Stream stateProtocolStream =
                    OpenUniqueProtocolFileStream(protocolFileDirectory, 
                    Character.Name ?? "UnnamedCharacter", "states", 
                    fileSystem);
                SaveProtocolToCsv(States, v => v, stateProtocolStream, 
                    "time", "state");
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("Saving the protocol " +
                    "of health points failed.", exc);
            }
        }

        private static Stream OpenUniqueProtocolFileStream(
            FileSystemPath protocolFileDirectory,
            string characterName, string logType, IFileSystem fileSystem)
        {
            if (!protocolFileDirectory.IsAbsolute ||
                !protocolFileDirectory.IsDirectoryPath)
                throw new ArgumentException("The specified protocol file " +
                    "directory path is no valid absolute directory path.");
            if (!fileSystem.IsWritable)
                throw new ArgumentException("The specified file system " +
                    "isn't writable.");

            string fileNameBase = protocolFileDirectory + 
                $"{characterName.ToLowerInvariant()}_" +
                $"{logType}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            const string fileNameSuffix = ".csv";

            string fileName = $"{fileNameBase}{fileNameSuffix}";

            for (int i = 0; !fileSystem.ExistsFile(fileName) && i < 999; i++)
                fileName = $"{fileNameBase}_{i}{fileNameSuffix}";

            if (fileSystem.ExistsFile(fileName))
                throw new InvalidOperationException("No unique file name " +
                    "could be found.");
            else return fileSystem.CreateFile(fileName, false);
        }

        private static void SaveProtocolToCsv<T>(
            SortedList<TimeSpan, T> protocol,
            Func<T, string> valueToString,
            Stream targetStream,
            string keyHeader, string valueHeader)
        {
            using StreamWriter writer = new StreamWriter(targetStream);

            writer.WriteLine($"{keyHeader};{valueHeader}");

            foreach (var protocolElement in protocol)
            {
                string timeString = 
                    protocolElement.Key.ToString("hh\\:mm\\:ss");
                string valueString = valueToString(protocolElement.Value);

                writer.WriteLine($"{timeString};{valueString}");
            }

            writer.Flush();
        }
    }
}
