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
            var population = new Population("mario.jpg", 5);
            population.OptimizePopulation(100000);
        }
    }

    class Population
    {
        public Genome srcGenome;
        public int parentPoolSize;
        public int genomePoolSize;
        public List<Genome> currentGenomes;
        public List<Genome> offspringGenomes;

        public Population(string srcFilepath, int parentPoolSize = 16)
        {
            this.srcGenome = this.CreateSourceGenome(srcFilepath);
            this.parentPoolSize = parentPoolSize;
            this.genomePoolSize = Convert.ToInt32(Math.Pow(parentPoolSize, 2));
            this.InitializeSeedGenomes();
        }

        private Genome CreateSourceGenome(string srcFilepath)
        {
            var srcGenome = new Genome(srcFilepath);
            return srcGenome;
        }

        private void InitializeSeedGenomes()
        {
            ConcurrentBag<Genome> seedGenomes = new ConcurrentBag<Genome>();
            Parallel.For(0, this.genomePoolSize, (index, state) =>
            {
                var seedGenome = new Genome(this.srcGenome, 0.01);
                seedGenomes.Add(seedGenome);
            });
            this.currentGenomes = seedGenomes.OrderBy(genome => genome.loss).ToList();
        }

        private List<Genome> SelectParents(List<Genome> targetGenomes)
        {
            return targetGenomes.GetRange(0, this.parentPoolSize);
        }

        private double CalculateGenomesLoss(List<Genome> targetGenomes)
        {
            var loss = targetGenomes.GetRange(0, parentPoolSize).Average(genome => genome.loss);
            return loss;
        }

        private void GenerateOffspringGeneration()
        {
            var parentGenomes = this.SelectParents(this.currentGenomes);
            ConcurrentBag<Genome> offspring = new ConcurrentBag<Genome>();
            Parallel.ForEach(parentGenomes, (father) =>
            {
                Parallel.ForEach(parentGenomes, (mother) =>
                {
                    var child = father.MateGenomes(mother);
                    offspring.Add(child);
                });
            });
            this.offspringGenomes = offspring.OrderBy(child => child.loss).ToList();
        }

        private void DecideGeneration()
        {
            var currentLoss = this.CalculateGenomesLoss(this.currentGenomes);
            var offspringLoss = this.CalculateGenomesLoss(this.offspringGenomes);

            if (offspringLoss < currentLoss)
            {
                this.currentGenomes = this.offspringGenomes;
            }
        }

        public void OptimizePopulation(int generations) {
            int n;
            for ( n = 0; n < generations; n++ )
            {
                if (n % 1000 == 0)
                {
                    var currentLoss = this.CalculateGenomesLoss(this.currentGenomes);
                    Console.WriteLine("generation {0} | loss {1}", n, currentLoss);
                    this.currentGenomes[0].SaveGenome("results/gen-" + n + ".bmp");
                }

                this.GenerateOffspringGeneration();
                this.DecideGeneration();
            }
            var finalLoss = this.CalculateGenomesLoss(this.currentGenomes);
            Console.WriteLine("generation {0} | loss {1}", n, finalLoss);
            this.currentGenomes[0].SaveGenome("results/gen-result.bmp");
        }
    }

    class Genome
    {
        public int width;
        public int height;
        public Genome srcGenome;
        public double mutation_rate;
        public Color [,] genes;
        public double loss;
        public List<Color> colors = new List<Color>()
        {
            Color.FromArgb(255, 255, 0, 0),
            Color.FromArgb(255, 0, 255, 0),
            Color.FromArgb(255, 0, 0, 255),
        };

        public Genome(
            Genome srcGenome,
            double mutation_rate
        )
        {
            this.width = srcGenome.width;
            this.height = srcGenome.height;
            this.srcGenome = srcGenome;
            this.mutation_rate = mutation_rate;
            this.genes = new Color [width, height];
            this.GenerateGenes();
        }

        public Genome(string srcFilepath)
        {
            this.LoadSource(srcFilepath);
        }

        public void SetSourceGenome(Genome srcGenome)
        {
            this.srcGenome = srcGenome;
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

        public Genome Clone()
        {
            return (Genome) this.MemberwiseClone();
        }

        public Genome MateGenomes(Genome partnerGenome)
        {
            Color [,] partnerGenes = partnerGenome.genes;
            Genome offspring = this.Clone();
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
                        int a = 255;
                        int r = rand.Next(0, 256);
                        int g = rand.Next(0, 256);
                        int b = rand.Next(0, 256);
                        Color randomColor = Color.FromArgb(a, r, g, b);
                        this.genes[w, h] = randomColor;
                    }
                }
            }
            this.CalculateLoss();
        }

    }
}
