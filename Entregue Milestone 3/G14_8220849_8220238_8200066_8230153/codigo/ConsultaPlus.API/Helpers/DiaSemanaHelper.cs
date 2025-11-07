namespace ConsultaPlus.API.Helpers
{
    public static class DiaSemanaHelper
    {
        public static string Normalizar(string? dia)
        {
            if (string.IsNullOrWhiteSpace(dia)) return dia ?? string.Empty;

            dia = dia.Trim().ToLowerInvariant();

            return dia switch
            {
                "seg" or "segunda" or "segunda-feira" => "Seg",
                "ter" or "terça" or "terca" or "terça-feira" or "terca-feira" => "Ter",
                "qua" or "quarta" or "quarta-feira" => "Qua",
                "qui" or "quinta" or "quinta-feira" => "Qui",
                "sex" or "sexta" or "sexta-feira" => "Sex",
                "sab" or "sábado" or "sabado" => "Sab",
                "dom" or "domingo" => "Dom",
                _ => dia 
            };
        }
    }
}
