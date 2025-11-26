using System;
using System.Collections.Generic;
using System.Linq;

namespace Organizainador.Utilities
{
    /// <summary>
    /// Clase auxiliar para operaciones relacionadas con horarios y gestión de tiempo.
    /// </summary>
    public static class HorarioHelper
    {
        /// <summary>
        /// Genera una lista de objetos TimeSpan en intervalos de 15 minutos, 
        /// comenzando desde el siguiente intervalo de 15 minutos de la hora actual.
        /// </summary>
        /// <returns>Una lista de TimeSpans (HH:mm:00) para selección en formularios.</returns>
        public static List<TimeSpan> GenerarHorasEnIntervalos()
        {
            var horaActual = DateTime.Now.TimeOfDay;
            var horasDisponibles = new List<TimeSpan>();

            // 1. Calcular el punto de partida (la hora actual redondeada al siguiente intervalo de 15 minutos)
            var totalMinutos = (int)horaActual.TotalMinutes;

            // Encuentra el siguiente múltiplo de 15
            var minutosRestantes = totalMinutos % 15;
            var minutosParaSiguiente = 15 - minutosRestantes;

            // Si la hora actual es exactamente un múltiplo de 15, el siguiente intervalo es 15 minutos después.
            if (minutosRestantes == 0)
            {
                minutosParaSiguiente = 15;
            }

            var horaInicio = horaActual.Add(TimeSpan.FromMinutes(minutosParaSiguiente));

            // 2. Generar todas las horas en intervalos de 15 minutos
            for (int h = 0; h < 24; h++)
            {
                for (int m = 0; m < 60; m += 15)
                {
                    horasDisponibles.Add(new TimeSpan(h, m, 0));
                }
            }

            // 3. Reordenar la lista para que el punto de partida esté primero
            var listaOrdenada = horasDisponibles
                // Filtra solo los horarios que son mayores o iguales al punto de partida (en el día de hoy)
                .Where(t => t >= horaInicio)
                .ToList();

            // Agrega los horarios anteriores al punto de partida (que serán los del día siguiente)
            listaOrdenada.AddRange(
                horasDisponibles
                .Where(t => t < horaInicio)
                .ToList()
            );

            return listaOrdenada;
        }
    }
}