namespace ElitePOS.Services
{
    public class ValidacionDocumentosService : IValidacionDocumentosService
    {
        /// <summary>
        /// Simula la validación de RUC/DNI
        /// TODO: En producción, integrar con API de SUNAT (https://api.sunat.gob.pe)
        /// </summary>
        public async Task<ValidacionDocumentoResult> ValidarDocumento(string numeroDocumento)
        {
            await Task.Delay(800); // Simular delay de API real

            Console.WriteLine($"🔍 Validando documento: {numeroDocumento}");

            // Validar formato
            if (string.IsNullOrWhiteSpace(numeroDocumento))
            {
                return new ValidacionDocumentoResult
                {
                    esValido = false,
                    mensaje = "Ingrese un número de documento"
                };
            }

            // Limpiar caracteres no numéricos
            numeroDocumento = new string(numeroDocumento.Where(char.IsDigit).ToArray());

            // VALIDACIÓN DE RUC (11 dígitos)
            if (numeroDocumento.Length == 11)
            {
                if (!EsRucValido(numeroDocumento))
                {
                    return new ValidacionDocumentoResult
                    {
                        esValido = false,
                        tipoDocumento = "RUC",
                        mensaje = "RUC inválido: formato incorrecto"
                    };
                }

                // SIMULACIÓN: En producción, consultar API de SUNAT
                return new ValidacionDocumentoResult
                {
                    esValido = true,
                    tipoDocumento = "RUC",
                    razonSocial = $"EMPRESA DEMO {numeroDocumento.Substring(0, 4)}",
                    estado = "ACTIVO",
                    direccion = "AV. DEMO 123 - LIMA - PERU",
                    mensaje = "✅ RUC válido (Simulación)"
                };
            }

            // VALIDACIÓN DE DNI (8 dígitos)
            else if (numeroDocumento.Length == 8)
            {
                if (!EsDniValido(numeroDocumento))
                {
                    return new ValidacionDocumentoResult
                    {
                        esValido = false,
                        tipoDocumento = "DNI",
                        mensaje = "DNI inválido: debe tener 8 dígitos"
                    };
                }

                // SIMULACIÓN: En producción, consultar API de RENIEC
                return new ValidacionDocumentoResult
                {
                    esValido = true,
                    tipoDocumento = "DNI",
                    razonSocial = $"CLIENTE DEMO {numeroDocumento.Substring(0, 4)}",
                    estado = "ACTIVO",
                    direccion = "AV. DEMO 456 - LIMA - PERU",
                    mensaje = "✅ DNI válido (Simulación)"
                };
            }

            // Formato no reconocido
            return new ValidacionDocumentoResult
            {
                esValido = false,
                mensaje = "Formato inválido: debe ser DNI (8 dígitos) o RUC (11 dígitos)"
            };
        }

        /// <summary>
        /// Valida formato de RUC (11 dígitos, empieza con 10, 15, 16, 17 o 20)
        /// </summary>
        public bool EsRucValido(string ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc) || ruc.Length != 11)
                return false;

            // Verificar que sean solo números
            if (!ruc.All(char.IsDigit))
                return false;

            // Verificar que empiece con prefijo válido
            var prefijo = ruc.Substring(0, 2);
            var prefijosValidos = new[] { "10", "15", "16", "17", "20" };

            return prefijosValidos.Contains(prefijo);
        }

        /// <summary>
        /// Valida formato de DNI (8 dígitos)
        /// </summary>
        public bool EsDniValido(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni) || dni.Length != 8)
                return false;

            // Verificar que sean solo números
            return dni.All(char.IsDigit);
        }
    }
}


