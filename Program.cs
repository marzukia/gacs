using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace GeneticAlgorithim
{
    class Program
    {
        static void Main(string[] args)
        {
            var srcGenome = new Genome("mario.jpg");
            int parentPoolSize = 5;
            int genomePoolSize = Convert.ToInt32(Math.Pow(parentPoolSize, 2));
            double mutationRate = 0.10;

            List<Genome> currentGenomes = GenerateStartingGenomes(srcGenome, genomePoolSize, mutationRate);

            int n;
            for ( n = 0; n < 1000000; n++ )
            {
                List<Genome> parentGenomes = SelectParents(currentGenomes, parentPoolSize);
                List<Genome> offspringGenomes = GenerateOffspringGenomes(parentGenomes);

                if ( n % 100 == 0 )
                {
                    Console.WriteLine(
                        "gen={0}, loss={1}, pool={2}, parents={3}",
                        n, currentGenomes[0].loss, currentGenomes.Count,
                        parentGenomes.Count
                    );
                    currentGenomes[0].SaveGenome("results/" + n + ".bmp"
                    );
                }

                if (offspringGenomes[0].loss < currentGenomes[0].loss)
                {
                    currentGenomes = offspringGenomes;
                }
            }
            currentGenomes[0].SaveGenome("results/final.bmp");
        }

        static List<Genome> GenerateStartingGenomes(Genome srcGenome, int genomePoolSize, double mutationRate)
        {
            ConcurrentBag<Genome> startingGenomes = new ConcurrentBag<Genome>();
            Parallel.For(0, genomePoolSize, (index, state) =>
            {
                var newGenome = new Genome(srcGenome.width, srcGenome.height, srcGenome, mutationRate);
                startingGenomes.Add(newGenome);
            });
            return startingGenomes.OrderBy(genome => genome.loss).ToList();
        }

        static Genome MateGenomes(Genome fatherGenome, Genome motherGenome)
        {
            var offspring = fatherGenome.MateGenomes(motherGenome);
            return offspring;
        }

        static List<Genome> GenerateOffspringGenomes(List<Genome> parentGenomes)
        {
            ConcurrentBag<Genome> offspring = new ConcurrentBag<Genome>();
            Parallel.ForEach(parentGenomes, (father) =>
            {
                Parallel.ForEach(parentGenomes, (mother) =>
                {
                    var child = MateGenomes(father, mother);
                    offspring.Add(child);
                });
            });
            return offspring.OrderBy(child => child.loss).ToList();
        }

        static List<Genome> SelectParents(List<Genome> parentGenomes, int parentPoolSize)
        {
            return parentGenomes.GetRange(0, parentPoolSize);
        }
    }

    class Genome
    {
        public int width { get; set; }
        public int height { get; set; }
        public Genome srcGenome { get; set; }
        public Genome parentGenome { get; set; }
        public double mutation_rate { get; set; }
        public Color [,] genes { get; set; }
        public double loss { get; set; }
        public List<Color> colors = new List<Color>()
        {
            Color.FromArgb(255, 255, 0, 0),
            Color.FromArgb(255, 0, 255, 0),
            Color.FromArgb(255, 0, 0, 255),
        };

        public Genome(
            int height,
            int width,
            Genome srcGenome,
            double mutation_rate = 0.01
        )
        {
            this.width = width;
            this.height = height;
            this.srcGenome = srcGenome;
            this.mutation_rate = mutation_rate;
            this.genes = new Color [width, height];
            this.GenerateGenes();
        }

        public Genome(string srcFilepath)
        {
            this.mutation_rate = 0;
            LoadSource(srcFilepath);
        }

        public Genome(Genome parentGenome)
        {
            this.width = parentGenome.width;
            this.height = parentGenome.height;
            this.srcGenome = parentGenome.srcGenome;
            this.parentGenome = parentGenome;
            this.mutation_rate = parentGenome.mutation_rate;
            this.genes = parentGenome.genes;
            this.loss = parentGenome.loss;
        }

        public void LoadSource(string srcFilepath)
        {
            var src = new Bitmap(srcFilepath, true);
            this.width = src.Width;
            this.height = src.Height;
            this.genes = new Color [width, height];
            int w; int h;
            for ( w = 0; w < this.width; w++ )
            {
                for ( h = 0; h < this.height; h++ )
                {
                    this.genes[w, h] = src.GetPixel(w, h);
                }
            }
        }

        public void SaveGenome(string dstFilepath)
        {
            Bitmap result = new Bitmap(this.width, this.height);
            int w; int h;
            for ( w = 0; w < this.width; w++ )
            {
                for ( h = 0; h < this.height; h++ )
                {
                    result.SetPixel(w, h, this.genes[w, h]);
                }
            }
            result.Save(dstFilepath);
        }

        public void CalculateLoss()
        {
            Color [,] srcGenes = this.srcGenome.genes;
            double loss = 0;
            int w; int h;
            for ( w = 0; w < this.width; w++ )
            {
                for ( h = 0; h < this.height; h++ )
                {
                    var srcGene = srcGenes[w, h];
                    var targetGene = this.genes[w, h];
                    double r_loss = Math.Pow(srcGene.R - targetGene.R, 2);
                    double g_loss = Math.Pow(srcGene.G - targetGene.G, 2);
                    double b_loss = Math.Pow(srcGene.B - targetGene.B, 2);
                    double pixel_loss = Math.Sqrt(r_loss + g_loss + b_loss);
                    loss += pixel_loss;
                }
            }
            this.loss = Math.Round(loss, 4);
        }

        public void MutateGenes()
        {
            int w; int h;
            for ( w = 0; w < this.width; w++ )
            {
                for ( h = 0; h < this.height; h++ )
                {
                    Random rand = new Random();
                    double p = rand.NextDouble();
                    if (p <= this.mutation_rate)
                    {
                        int r = rand.Next(0, 256);
                        int g = rand.Next(0, 256);
                        int b = rand.Next(0, 256);
                        Color randomColor = Color.FromArgb(255, r, g, b);
                        this.genes[w, h] = randomColor;
                    }
                }
            }
            this.CalculateLoss();
        }

        public Genome MateGenomes(Genome partnerGenome)
        {
            Color [,] partnerGenes = partnerGenome.genes;
            Genome offspring = new Genome(this);
            int w; int h;
            for ( w = 0; w < this.width; w++ )
            {
                for ( h = 0; h < this.height; h++ )
                {
                    Random rand = new Random();
                    double p = rand.NextDouble();
                    if (p > 0.5)
                    {
                        offspring.genes[w, h] = partnerGenes[w, h];
                    }
                }
            }
            offspring.MutateGenes();
            return offspring;
        }

        private void GenerateGenes()
        {
            int w; int h;
            for ( w = 0; w < this.width; w++ )
            {
                for ( h = 0; h < this.height; h++ )
                {
                    int index = new Random().Next(this.colors.Count);
                    this.genes[w, h] = this.colors[index];
                }
            }
            this.CalculateLoss();
        }
    }
}
