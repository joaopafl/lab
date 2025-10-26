using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;

namespace Pi_Odonto.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Dashboard
        public IActionResult Dashboard()
        {
            ViewBag.TotalResponsaveis = _context.Responsaveis.Count();
            ViewBag.TotalCriancas = _context.Criancas.Count();
            ViewBag.TotalDentistas = _context.Dentistas.Count();
            ViewBag.TotalAgendamentos = _context.Agendamentos.Count();
            ViewBag.TotalSolicitacoesVoluntarios = _context.SolicitacoesVoluntario.Count();
            ViewBag.SolicitacoesNaoVisualizadas = _context.SolicitacoesVoluntario.Count(s => !s.Visualizado);

            return View();
        }

        // ========================================
        // GERENCIAMENTO DE SOLICITAÇÕES VOLUNTÁRIO
        // ========================================

        [HttpGet]
        public async Task<IActionResult> Solicitacoes(string filtro = "todas")
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var query = _context.SolicitacoesVoluntario.AsQueryable();

            switch (filtro.ToLower())
            {
                case "pendentes":
                    query = query.Where(s => s.Status == "Pendente");
                    break;
                case "aprovadas":
                    query = query.Where(s => s.Status == "Aprovado");
                    break;
                case "rejeitadas":
                    query = query.Where(s => s.Status == "Rejeitado");
                    break;
                case "nao_visualizadas":
                    query = query.Where(s => !s.Visualizado);
                    break;
            }

            var solicitacoes = await query
                .OrderByDescending(s => s.DataSolicitacao)
                .ToListAsync();

            ViewBag.Filtro = filtro;
            ViewBag.TotalPendentes = await _context.SolicitacoesVoluntario
                .CountAsync(s => s.Status == "Pendente");
            ViewBag.TotalNaoVisualizadas = await _context.SolicitacoesVoluntario
                .CountAsync(s => !s.Visualizado);

            return View(solicitacoes);
        }

        [HttpGet]
        public async Task<IActionResult> DetalhesSolicitacao(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var solicitacao = await _context.SolicitacoesVoluntario
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitacao == null)
            {
                TempData["Erro"] = "Solicitação não encontrada.";
                return RedirectToAction("Solicitacoes");
            }

            if (!solicitacao.Visualizado)
            {
                solicitacao.Visualizado = true;
                await _context.SaveChangesAsync();
            }

            return View(solicitacao);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprovarSolicitacao(int id, string observacao)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Acesso negado" });
            }

            try
            {
                var solicitacao = await _context.SolicitacoesVoluntario
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (solicitacao == null)
                {
                    return Json(new { success = false, message = "Solicitação não encontrada" });
                }

                solicitacao.Status = "Aprovado";
                solicitacao.DataResposta = DateTime.Now;
                solicitacao.ObservacaoAdmin = observacao;
                solicitacao.Visualizado = true;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Solicitação aprovada com sucesso!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Erro ao aprovar solicitação: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejeitarSolicitacao(int id, string observacao)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Acesso negado" });
            }

            try
            {
                var solicitacao = await _context.SolicitacoesVoluntario
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (solicitacao == null)
                {
                    return Json(new { success = false, message = "Solicitação não encontrada" });
                }

                solicitacao.Status = "Rejeitado";
                solicitacao.DataResposta = DateTime.Now;
                solicitacao.ObservacaoAdmin = observacao;
                solicitacao.Visualizado = true;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Solicitação rejeitada."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Erro ao rejeitar solicitação: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirSolicitacao(int id)
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado.";
                return RedirectToAction("Solicitacoes");
            }

            try
            {
                var solicitacao = await _context.SolicitacoesVoluntario
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (solicitacao != null)
                {
                    _context.SolicitacoesVoluntario.Remove(solicitacao);
                    await _context.SaveChangesAsync();
                    TempData["Sucesso"] = "Solicitação excluída com sucesso.";
                }
                else
                {
                    TempData["Erro"] = "Solicitação não encontrada.";
                }
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao excluir solicitação: {ex.Message}";
            }

            return RedirectToAction("Solicitacoes");
        }

        private bool IsAdmin()
        {
            return User.HasClaim("TipoUsuario", "Admin");
        }
    }
}