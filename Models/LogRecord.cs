using System;
using System.Collections.Generic;
using System.Text;

namespace Projet_EasyLog.Models
{
    /// <summary>
    /// Classe de base représentant un enregistrement de log générique.
    /// Elle sert de type parent pour garantir le polymorphisme : la méthode abstraite WriteLog accepte ce type,
    /// ce qui permet de passer indifféremment des JsonRecord ou d'autres possible futurs types de logs.
    /// </summary>
    public class LogRecord
    {
    }
}
