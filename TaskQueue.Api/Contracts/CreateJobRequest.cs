using System.ComponentModel.DataAnnotations;

namespace TaskQueue.Api.Contracts
{
    /// <summary>
    /// Dados necessários para criação de uma nova tarefa.
    /// </summary>
    public record CreateJobRequest
    {
        /// <summary>
        /// Tipo da tarefa a ser executada. Ex: EnviarEmail, GerarRelatorio, ProcessarPagamento.
        /// </summary>
        [Required]
        [MinLength(5, ErrorMessage = "Category deve ter no mínimo 5 caracteres.")]
        [MaxLength(100, ErrorMessage = "Category deve ter no máximo 100 caracteres.")]
        public string Category { get; init; } = string.Empty;

        /// <summary>
        /// Dados em formato JSON com as informações necessárias para executar a tarefa.
        /// </summary>
        [Required]
        [MinLength(7, ErrorMessage = "Payload deve ter no mínimo 7 caracteres.")]
        [MaxLength(2000, ErrorMessage = "Payload deve ter no máximo 2000 caracteres.")]
        public string Payload { get; init; } = string.Empty;
    }
}