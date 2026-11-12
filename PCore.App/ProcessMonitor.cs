using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PCore.App
{
    public sealed class ProcessMonitor
    {
        public IList<ProcessRow> Read()
        {
            var rows = new List<ProcessRow>();

            foreach (Process process in Process.GetProcesses())
            {
                using (process)
                {
                    try
                    {
                        rows.Add(new ProcessRow
                        {
                            Name = process.ProcessName,
                            Id = process.Id,
                            MemoryMb = process.WorkingSet64 / 1048576d,
                            CpuTime = process.TotalProcessorTime.ToString(@"hh\:mm\:ss"),
                            WindowTitle = string.IsNullOrWhiteSpace(process.MainWindowTitle)
                                ? "—"
                                : process.MainWindowTitle
                        });
                    }
                    catch (InvalidOperationException) { }
                    catch (System.ComponentModel.Win32Exception) { }
                    catch (NotSupportedException) { }
                }
            }

            return rows.OrderByDescending(row => row.MemoryMb).ToList();
        }

        public static bool TryStop(int processId, out string error)
        {
            error = null;
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    if (process.Id == Process.GetCurrentProcess().Id)
                    {
                        error = "Нельзя завершить сам PCore.";
                        return false;
                    }

                    process.Kill();
                    return true;
                }
            }
            catch (ArgumentException) { error = "Процесс уже завершён."; }
            catch (InvalidOperationException) { error = "Процесс уже завершён."; }
            catch (System.ComponentModel.Win32Exception) { error = "Недостаточно прав для завершения процесса."; }
            catch (NotSupportedException) { error = "Операция не поддерживается."; }

            return false;
        }
    }

    public sealed class ProcessRow
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public double MemoryMb { get; set; }
        public string CpuTime { get; set; }
        public string WindowTitle { get; set; }
    }
}