using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.ViewModels;

namespace Pi_Odonto.Controllers
{
    // CRÍTICO: Adicionar autorização para proteger todas as ações
    [Authorize] // Apenas usuários autenticados
    public class DentistaController : Controller
    {
        private readonly AppDbContext _context;

        public DentistaController(AppDbContext context)
        {
            _context = context;
        }

        // Método auxiliar para verificar se é Admin
        private bool IsAdmin()
        {
            return User.HasClaim("TipoUsuario", "Admin");
        }

        // GET: Dentista
        [Authorize] // Reforçando autorização
        public IActionResult Index()
        {
            // Verificar se é admin
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado. Apenas administradores podem gerenciar dentistas.";
                return RedirectToAction("Index", "Home");
            }

            var dentistas = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                .Include(d => d.Disponibilidades)
                .ToList();

            return View(dentistas);
        }

        // GET: Dentista/Create
        [Authorize]
        public IActionResult Create()
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado. Apenas administradores podem cadastrar dentistas.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Escalas = _context.EscalaTrabalho.ToList();

            var viewModel = new DentistaViewModel
            {
                Disponibilidades = ObterDisponibilidadesPadrao()
            };

            return View(viewModel);
        }

        // POST: Dentista/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Create(DentistaViewModel viewModel, int? IdEscala)
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                // Criar o dentista
                var dentista = new Dentista
                {
                    Nome = viewModel.Nome,
                    Cpf = viewModel.Cpf,
                    Cro = viewModel.Cro,
                    Endereco = viewModel.Endereco,
                    Email = viewModel.Email,
                    Telefone = viewModel.Telefone,
                    IdEscala = IdEscala
                };

                _context.Dentistas.Add(dentista);
                _context.SaveChanges();

                // Adicionar as disponibilidades selecionadas
                foreach (var disponibilidade in viewModel.Disponibilidades.Where(d => d.Selecionado))
                {
                    var novaDisponibilidade = new DisponibilidadeDentista
                    {
                        IdDentista = dentista.Id,
                        DiaSemana = disponibilidade.DiaSemana,
                        HoraInicio = disponibilidade.HoraInicio,
                        HoraFim = disponibilidade.HoraFim
                    };

                    _context.DisponibilidadesDentista.Add(novaDisponibilidade);
                }

                _context.SaveChanges();
                TempData["Sucesso"] = "Dentista cadastrado com sucesso!";
                return RedirectToAction("Index");
            }

            ViewBag.Escalas = _context.EscalaTrabalho.ToList();
            viewModel.Disponibilidades = ObterDisponibilidadesPadrao();
            return View(viewModel);
        }

        // GET: Dentista/Edit/5
        [Authorize]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            var dentista = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                .Include(d => d.Disponibilidades)
                .FirstOrDefault(d => d.Id == id);

            if (dentista == null) return NotFound();

            ViewBag.Escalas = _context.EscalaTrabalho.ToList();

            var viewModel = new DentistaViewModel
            {
                Id = dentista.Id,
                Nome = dentista.Nome,
                Cpf = dentista.Cpf,
                Cro = dentista.Cro,
                Endereco = dentista.Endereco,
                Email = dentista.Email,
                Telefone = dentista.Telefone,
                IdEscala = dentista.IdEscala,
                Disponibilidades = ObterDisponibilidadesComSelecoes(dentista.Disponibilidades)
            };

            return View(viewModel);
        }

        // POST: Dentista/Edit/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(DentistaViewModel viewModel, int? IdEscala)
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                var dentista = _context.Dentistas.FirstOrDefault(d => d.Id == viewModel.Id);
                if (dentista == null) return NotFound();

                dentista.Nome = viewModel.Nome;
                dentista.Cpf = viewModel.Cpf;
                dentista.Cro = viewModel.Cro;
                dentista.Endereco = viewModel.Endereco;
                dentista.Email = viewModel.Email;
                dentista.Telefone = viewModel.Telefone;
                dentista.IdEscala = IdEscala;

                _context.Dentistas.Update(dentista);

                // Remover disponibilidades existentes
                var disponibilidadesExistentes = _context.DisponibilidadesDentista
                    .Where(d => d.IdDentista == dentista.Id).ToList();
                _context.DisponibilidadesDentista.RemoveRange(disponibilidadesExistentes);

                // Adicionar novas disponibilidades
                foreach (var disponibilidade in viewModel.Disponibilidades.Where(d => d.Selecionado))
                {
                    var novaDisponibilidade = new DisponibilidadeDentista
                    {
                        IdDentista = dentista.Id,
                        DiaSemana = disponibilidade.DiaSemana,
                        HoraInicio = disponibilidade.HoraInicio,
                        HoraFim = disponibilidade.HoraFim
                    };

                    _context.DisponibilidadesDentista.Add(novaDisponibilidade);
                }

                _context.SaveChanges();
                TempData["Sucesso"] = "Dentista atualizado com sucesso!";
                return RedirectToAction("Index");
            }

            ViewBag.Escalas = _context.EscalaTrabalho.ToList();
            return View(viewModel);
        }

        // GET: Dentista/Delete/5
        [Authorize]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            var dentista = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                .FirstOrDefault(d => d.Id == id);

            if (dentista == null) return NotFound();
            return View(dentista);
        }

        // POST: Dentista/DeleteConfirmed/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            var dentista = _context.Dentistas.Find(id);
            if (dentista != null)
            {
                _context.Dentistas.Remove(dentista);
                _context.SaveChanges();
                TempData["Sucesso"] = "Dentista removido com sucesso!";
            }

            return RedirectToAction("Index");
        }

        // GET: Dentista/Details/5
        [Authorize]
        public IActionResult Details(int id)
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            var dentista = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                .Include(d => d.Disponibilidades)
                .FirstOrDefault(d => d.Id == id);

            if (dentista == null) return NotFound();
            return View(dentista);
        }

        // Métodos auxiliares - MUDANÇA AQUI: DisponibilidadeItem ao invés de DisponibilidadeViewModel
        private List<DisponibilidadeItem> ObterDisponibilidadesPadrao()
        {
            var disponibilidades = new List<DisponibilidadeItem>();
            var diasSemana = new[] { "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado" };
            var horarios = new[]
            {
                (TimeSpan.FromHours(8), TimeSpan.FromHours(12)),
                (TimeSpan.FromHours(14), TimeSpan.FromHours(18))
            };

            foreach (var dia in diasSemana)
            {
                foreach (var (inicio, fim) in horarios)
                {
                    disponibilidades.Add(new DisponibilidadeItem
                    {
                        DiaSemana = dia,
                        HoraInicio = inicio,
                        HoraFim = fim,
                        Selecionado = false
                    });
                }
            }

            return disponibilidades;
        }

        private List<DisponibilidadeItem> ObterDisponibilidadesComSelecoes(
            ICollection<DisponibilidadeDentista> disponibilidadesExistentes)
        {
            var todasDisponibilidades = ObterDisponibilidadesPadrao();

            foreach (var disp in todasDisponibilidades)
            {
                disp.Selecionado = disponibilidadesExistentes.Any(d =>
                    d.DiaSemana == disp.DiaSemana &&
                    d.HoraInicio == disp.HoraInicio &&
                    d.HoraFim == disp.HoraFim);
            }

            return todasDisponibilidades;
        }
    }
}