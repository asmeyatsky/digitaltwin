using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class LearningService : ILearningService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<LearningService> _logger;

        public LearningService(DigitalTwinDbContext context, ILogger<LearningService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<LearningPath>> GetPathsAsync(LearningCategory? category)
        {
            var query = _context.LearningPaths.AsQueryable();

            if (category.HasValue)
                query = query.Where(p => p.Category == category.Value);

            return await query.OrderBy(p => p.Title).ToListAsync();
        }

        public async Task<(LearningPath? Path, List<LearningModule> Modules)> GetPathByIdAsync(Guid pathId)
        {
            var path = await _context.LearningPaths.FindAsync(pathId);
            if (path == null)
                return (null, new List<LearningModule>());

            var modules = await _context.LearningModules
                .Where(m => m.PathId == pathId)
                .OrderBy(m => m.Order)
                .ToListAsync();

            return (path, modules);
        }

        public async Task<UserLearningProgress> StartPathAsync(Guid userId, Guid pathId)
        {
            var path = await _context.LearningPaths.FindAsync(pathId);
            if (path == null)
                throw new InvalidOperationException("Learning path not found.");

            var existing = await _context.UserLearningProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.PathId == pathId);

            if (existing != null)
                return existing;

            var progress = new UserLearningProgress
            {
                UserId = userId,
                PathId = pathId,
                CurrentModuleIndex = 0,
                CompletedModules = "[]",
                ReflectionNotes = "{}",
                StartedAt = DateTime.UtcNow
            };

            _context.UserLearningProgress.Add(progress);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} started learning path {PathId}", userId, pathId);
            return progress;
        }

        public async Task<(LearningModule? Module, UserLearningProgress? Progress)> GetCurrentModuleAsync(Guid userId, Guid pathId)
        {
            var progress = await _context.UserLearningProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.PathId == pathId);

            if (progress == null)
                return (null, null);

            var module = await _context.LearningModules
                .Where(m => m.PathId == pathId && m.Order == progress.CurrentModuleIndex)
                .FirstOrDefaultAsync();

            return (module, progress);
        }

        public async Task<UserLearningProgress> CompleteModuleAsync(Guid userId, Guid pathId, string? reflectionNotes)
        {
            var progress = await _context.UserLearningProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.PathId == pathId);

            if (progress == null)
                throw new InvalidOperationException("User has not started this learning path.");

            if (progress.CompletedAt.HasValue)
                throw new InvalidOperationException("This learning path is already completed.");

            // Update completed modules list
            var completedModules = JsonSerializer.Deserialize<List<int>>(progress.CompletedModules) ?? new List<int>();
            if (!completedModules.Contains(progress.CurrentModuleIndex))
                completedModules.Add(progress.CurrentModuleIndex);
            progress.CompletedModules = JsonSerializer.Serialize(completedModules);

            // Save reflection notes if provided
            if (!string.IsNullOrWhiteSpace(reflectionNotes))
            {
                var notes = JsonSerializer.Deserialize<Dictionary<string, string>>(progress.ReflectionNotes) ?? new Dictionary<string, string>();
                notes[progress.CurrentModuleIndex.ToString()] = reflectionNotes;
                progress.ReflectionNotes = JsonSerializer.Serialize(notes);
            }

            // Check if the path is complete
            var path = await _context.LearningPaths.FindAsync(pathId);
            if (path != null && completedModules.Count >= path.ModuleCount)
            {
                progress.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("User {UserId} completed learning path {PathId}", userId, pathId);
            }
            else
            {
                // Advance to the next module
                progress.CurrentModuleIndex++;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} completed module {ModuleIndex} of path {PathId}", userId, progress.CurrentModuleIndex - 1, pathId);
            return progress;
        }

        public async Task<List<UserLearningProgress>> GetProgressAsync(Guid userId)
        {
            return await _context.UserLearningProgress
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.StartedAt)
                .ToListAsync();
        }

        public async Task<LearningPath?> GetSuggestedPathAsync(Guid userId)
        {
            // Get user's progress to find least-explored categories
            var userProgress = await _context.UserLearningProgress
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var startedPathIds = userProgress.Select(p => p.PathId).ToHashSet();

            // Count how many paths the user has started per category
            var startedPaths = await _context.LearningPaths
                .Where(p => startedPathIds.Contains(p.Id))
                .ToListAsync();

            var categoryCounts = startedPaths
                .GroupBy(p => p.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            // Find all available paths the user hasn't completed
            var completedPathIds = userProgress
                .Where(p => p.CompletedAt.HasValue)
                .Select(p => p.PathId)
                .ToHashSet();

            var availablePaths = await _context.LearningPaths
                .Where(p => !completedPathIds.Contains(p.Id))
                .ToListAsync();

            if (!availablePaths.Any())
                return null;

            // Prefer paths from least-explored categories, and paths not yet started
            var suggested = availablePaths
                .OrderBy(p => categoryCounts.GetValueOrDefault(p.Category, 0))
                .ThenBy(p => startedPathIds.Contains(p.Id) ? 1 : 0)
                .FirstOrDefault();

            return suggested;
        }

        public async Task SeedLearningContentAsync()
        {
            // Only seed if no paths exist
            if (await _context.LearningPaths.AnyAsync())
                return;

            _logger.LogInformation("Seeding learning content...");

            // Path 1: Understanding Your Emotions
            var path1 = new LearningPath
            {
                Title = "Understanding Your Emotions",
                Description = "Build your emotional literacy by learning to identify, name, and understand your emotions. This path helps you develop a richer vocabulary for your inner experience.",
                Category = LearningCategory.EmotionalIntelligence,
                EstimatedMinutes = 30,
                ModuleCount = 4
            };

            var path1Modules = new List<LearningModule>
            {
                new LearningModule
                {
                    PathId = path1.Id,
                    Title = "The Emotion Wheel",
                    Content = "Emotions are complex experiences that go far beyond simply feeling \"good\" or \"bad.\" Psychologist Robert Plutchik developed the Emotion Wheel, which identifies eight basic emotions: joy, trust, fear, surprise, sadness, disgust, anger, and anticipation. Each of these has varying intensities -- for example, annoyance is a milder form of anger, while rage is its most intense form.\n\nBasic emotions are universal and hardwired into our biology. Complex emotions, on the other hand, are blends of basic emotions. For example, love can be seen as a combination of joy and trust, while contempt might blend anger and disgust.\n\nUnderstanding this spectrum helps you move beyond simple labels and recognize the nuanced landscape of your emotional life. When you can pinpoint exactly what you're feeling, you gain more power to respond thoughtfully rather than react impulsively.",
                    ExercisePrompt = "Identify 3 emotions you felt today. For each one, try to determine: Was it a basic emotion or a blend? What intensity level was it? What triggered it?",
                    Order = 0
                },
                new LearningModule
                {
                    PathId = path1.Id,
                    Title = "Emotional Triggers",
                    Content = "An emotional trigger is any stimulus -- a person, situation, memory, or even a smell -- that provokes a strong emotional response. Triggers are often connected to past experiences, particularly those from childhood or significant life events.\n\nCommon triggers include feeling ignored or dismissed, perceived criticism, feeling out of control, being compared to others, or experiencing injustice. Your triggers are unique to your personal history and are neither good nor bad -- they are simply data about what matters to you.\n\nRecognizing your triggers is the first step toward managing your emotional responses. When you notice a disproportionately strong reaction, pause and ask yourself: \"What just happened? What does this remind me of? What need of mine feels threatened?\" This awareness creates a gap between stimulus and response, giving you the freedom to choose how you want to react.",
                    ExercisePrompt = "Write about a recent emotional trigger. What happened? What emotion arose? Can you trace it back to a deeper need or past experience?",
                    Order = 1
                },
                new LearningModule
                {
                    PathId = path1.Id,
                    Title = "Emotion vs Reaction",
                    Content = "There is an important distinction between feeling an emotion and acting on it. Emotions themselves are never wrong -- they are natural signals that provide information about our needs and values. However, our reactions to emotions can range from helpful to harmful.\n\nThe space between feeling and reacting is where emotional intelligence lives. Viktor Frankl wrote: \"Between stimulus and response there is a space. In that space is our freedom and our power to choose our response.\"\n\nPracticing this pause involves several steps: First, notice the emotion arising in your body (tightness in your chest, heat in your face, a sinking feeling in your stomach). Second, name the emotion without judgment. Third, take a breath and consider your options. Fourth, choose a response that aligns with your values.\n\nThis doesn't mean suppressing emotions. Suppression leads to emotional buildup and eventual explosion. Instead, acknowledge the emotion fully while choosing your behavioral response deliberately.",
                    ExercisePrompt = "Recall a time you paused before reacting to a strong emotion. What helped you create that pause? If you can't recall one, imagine a recent situation where a pause would have changed the outcome.",
                    Order = 2
                },
                new LearningModule
                {
                    PathId = path1.Id,
                    Title = "Emotional Vocabulary",
                    Content = "Research shows that people who can name their emotions with precision -- a skill called emotional granularity -- experience better emotional regulation, make better decisions, and have stronger relationships.\n\nConsider the difference between saying \"I feel bad\" versus \"I feel disappointed because I had high expectations\" or \"I feel overwhelmed because I have too many commitments.\" The more specific your emotional vocabulary, the clearer your understanding of what you need.\n\nHere are some emotion words to expand your vocabulary:\n\n- Instead of \"happy\": content, elated, grateful, hopeful, peaceful, proud, relieved, inspired\n- Instead of \"sad\": lonely, disappointed, grieving, melancholy, nostalgic, helpless, discouraged\n- Instead of \"angry\": frustrated, resentful, irritated, betrayed, indignant, exasperated\n- Instead of \"scared\": anxious, vulnerable, insecure, overwhelmed, apprehensive, dread\n- Instead of \"fine\": numb, neutral, ambivalent, restless, curious, cautiously optimistic\n\nBuilding this vocabulary is like upgrading from a box of 8 crayons to a set of 64. The richer your palette, the more accurately you can color your emotional experience.",
                    ExercisePrompt = "Think of 5 recent situations where you described your feelings as \"fine,\" \"good,\" or \"bad.\" Replace each with a more specific emotion word. How does the more precise label change your understanding of those moments?",
                    Order = 3
                }
            };

            // Path 2: Mindfulness Basics
            var path2 = new LearningPath
            {
                Title = "Mindfulness Basics",
                Description = "Learn the foundations of mindfulness practice. Discover how present-moment awareness can reduce stress, improve focus, and deepen your connection to daily life.",
                Category = LearningCategory.Mindfulness,
                EstimatedMinutes = 25,
                ModuleCount = 4
            };

            var path2Modules = new List<LearningModule>
            {
                new LearningModule
                {
                    PathId = path2.Id,
                    Title = "What Is Mindfulness?",
                    Content = "Mindfulness is the practice of paying attention to the present moment with curiosity and without judgment. It sounds simple, but our minds are naturally wired to wander -- replaying the past, planning the future, or generating a constant stream of commentary about our experience.\n\nJon Kabat-Zinn, who pioneered Mindfulness-Based Stress Reduction (MBSR), defines mindfulness as \"paying attention in a particular way: on purpose, in the present moment, and non-judgmentally.\"\n\nMindfulness is not about emptying your mind or achieving a state of bliss. It is about developing a different relationship with your thoughts and feelings -- one where you observe them without being swept away. Think of your mind like a sky and your thoughts like clouds: mindfulness helps you become the sky rather than getting lost in each cloud.\n\nResearch has shown that regular mindfulness practice can reduce stress and anxiety, improve attention and focus, enhance emotional regulation, boost immune function, and increase overall well-being. Even a few minutes a day can make a meaningful difference.",
                    ExercisePrompt = "Spend 2 minutes simply noticing your breath. Don't try to change it -- just observe. Notice the sensation of air entering and leaving your body. When your mind wanders (and it will), gently bring your attention back to the breath without self-criticism.",
                    Order = 0
                },
                new LearningModule
                {
                    PathId = path2.Id,
                    Title = "Body Scan",
                    Content = "A body scan is a foundational mindfulness practice that involves systematically directing your attention through different parts of your body. It helps you develop awareness of physical sensations, notice where you hold tension, and cultivate a deeper mind-body connection.\n\nHow to do a body scan:\n1. Find a comfortable position, either lying down or sitting.\n2. Close your eyes or soften your gaze.\n3. Start at the top of your head. Notice any sensations -- tingling, warmth, pressure, or nothing at all.\n4. Slowly move your attention down through your face, neck, shoulders, arms, hands, chest, abdomen, hips, legs, and feet.\n5. At each area, simply notice what you feel without trying to change it.\n6. If you notice tension, breathe into that area and imagine it softening.\n7. If your mind wanders, gently redirect it to the body part you were scanning.\n\nMany people discover that they carry tension in specific areas without realizing it -- tight shoulders, a clenched jaw, a tight stomach. Regular body scans help you catch this tension early and release it before it accumulates.",
                    ExercisePrompt = "Do a 3-minute body scan right now. Start from the top of your head and slowly move your attention down to your toes. Notice any areas of tension, comfort, or numbness. What did you discover?",
                    Order = 1
                },
                new LearningModule
                {
                    PathId = path2.Id,
                    Title = "Mindful Observation",
                    Content = "Mindful observation is the practice of giving your full attention to something you normally take for granted. It trains your brain to notice details, find beauty in the ordinary, and anchor yourself in the present moment.\n\nWe spend much of our lives on autopilot, moving through routines without truly seeing, hearing, or feeling our surroundings. Mindful observation interrupts this pattern and reconnects you with the richness of direct experience.\n\nYou can practice mindful observation with anything:\n- A cup of tea: Notice its color, warmth, aroma, and the sensation of the first sip.\n- A tree outside your window: Observe its shape, the texture of its bark, the movement of its leaves.\n- Your hands: Look at the lines, the texture of your skin, the way they move.\n- Sounds around you: Close your eyes and identify each sound layer by layer.\n\nThe key is to observe without labeling or judging. Instead of thinking \"that's a bird singing,\" try to hear the actual quality of the sound -- its pitch, rhythm, and texture. This shifts you from thinking about experience to directly experiencing.\n\nThis practice builds your capacity for presence, which naturally reduces rumination and anxiety.",
                    ExercisePrompt = "Choose one object near you and observe it for 60 seconds with full attention. Notice its shape, color, texture, weight, and any details you've never noticed before. What did you see that surprised you?",
                    Order = 2
                },
                new LearningModule
                {
                    PathId = path2.Id,
                    Title = "Daily Mindfulness",
                    Content = "The real power of mindfulness comes not from formal meditation sessions but from integrating mindful awareness into your daily life. Any routine activity can become a mindfulness practice when you bring your full attention to it.\n\nHere are ways to weave mindfulness into your day:\n\n**Mindful mornings:** Before checking your phone, take three conscious breaths. Notice how your body feels. Set an intention for the day.\n\n**Mindful eating:** For one meal, eat without screens. Notice the colors, textures, and flavors of your food. Chew slowly. Notice when you feel satisfied.\n\n**Mindful walking:** During a short walk, feel each step -- the contact of your foot with the ground, the movement of your legs, the air on your skin.\n\n**Mindful transitions:** Use transitions between activities (getting in the car, entering a building, opening your laptop) as mindfulness cues. Pause, take a breath, and arrive fully in the new moment.\n\n**Mindful listening:** In your next conversation, give the other person your complete attention. Notice their words, tone, and body language without planning your response.\n\nStart with just one activity and practice it mindfully for a week before adding another. The goal is not perfection but a gradual shift toward more present, intentional living.",
                    ExercisePrompt = "Choose one daily activity you normally do on autopilot (brushing teeth, washing dishes, walking to work). Commit to doing it mindfully for the next three days. What activity will you choose, and what do you hope to notice?",
                    Order = 3
                }
            };

            // Path 3: Better Communication
            var path3 = new LearningPath
            {
                Title = "Better Communication",
                Description = "Strengthen your relationships through effective communication skills. Learn active listening, assertiveness, and conflict resolution techniques.",
                Category = LearningCategory.Communication,
                EstimatedMinutes = 35,
                ModuleCount = 5
            };

            var path3Modules = new List<LearningModule>
            {
                new LearningModule
                {
                    PathId = path3.Id,
                    Title = "Active Listening",
                    Content = "Active listening is one of the most powerful communication skills you can develop, yet it is surprisingly rare. Most of us listen with the intent to reply rather than to understand. Active listening means giving your full attention to the speaker and seeking to understand their message and feelings.\n\nThe components of active listening:\n\n**Be present:** Put away distractions. Make eye contact (in a comfortable, not staring, way). Orient your body toward the speaker.\n\n**Listen to understand:** Focus on what the person is saying rather than formulating your response. Let go of the urge to fix, advise, or relate their experience to your own.\n\n**Reflect back:** Paraphrase what you heard: \"It sounds like you're saying...\" or \"So you feel...\" This shows the speaker they've been heard and gives them a chance to clarify.\n\n**Ask open-ended questions:** \"Can you tell me more about that?\" \"How did that make you feel?\" \"What would be most helpful for you right now?\"\n\n**Validate emotions:** \"That sounds really frustrating\" or \"I can understand why you'd feel that way.\" You don't have to agree with someone to validate their feelings.\n\nWhen people feel truly heard, trust deepens, conflicts de-escalate, and connections strengthen. Active listening is a gift you give another person -- the gift of your full attention.",
                    ExercisePrompt = "In your next conversation, practice active listening for 5 minutes. Focus entirely on the other person, reflect back what you hear, and resist the urge to share your own stories or give advice. What did you notice about the conversation?",
                    Order = 0
                },
                new LearningModule
                {
                    PathId = path3.Id,
                    Title = "I-Statements",
                    Content = "When we feel hurt, frustrated, or angry, it is natural to express those feelings as accusations: \"You never listen to me,\" \"You always cancel plans,\" \"You make me so angry.\" These \"you-statements\" put the other person on the defensive and often escalate conflict.\n\nI-statements are a powerful alternative. They express your feelings and needs without blaming the other person. The basic formula is:\n\n\"I feel [emotion] when [specific behavior] because [impact on you].\"\n\nExamples:\n- Instead of \"You never help around the house,\" try \"I feel overwhelmed when I do all the chores alone because I don't have time to rest.\"\n- Instead of \"You're always on your phone,\" try \"I feel disconnected when we're together but not talking because I value our time together.\"\n- Instead of \"You don't care about me,\" try \"I feel unimportant when my calls aren't returned because connection with you matters to me.\"\n\nI-statements work because they:\n1. Take ownership of your emotions (nobody \"makes\" you feel anything)\n2. Focus on specific behaviors rather than character attacks\n3. Explain the impact, helping the other person understand why it matters\n4. Open the door for problem-solving rather than blame\n\nThis is not about being passive or avoiding difficult conversations. It is about having those conversations in a way that invites collaboration rather than combat.",
                    ExercisePrompt = "Think of 3 complaints or frustrations you have with someone (a partner, friend, colleague, or family member). Rewrite each one as an I-statement using the formula: \"I feel [emotion] when [behavior] because [impact].\"",
                    Order = 1
                },
                new LearningModule
                {
                    PathId = path3.Id,
                    Title = "Nonverbal Communication",
                    Content = "Research suggests that 55-93% of communication is nonverbal. Your body language, facial expressions, tone of voice, and even your physical distance from someone all send powerful messages -- sometimes contradicting your words.\n\nKey aspects of nonverbal communication:\n\n**Body language:** Crossed arms can signal defensiveness. Leaning in shows interest. Mirroring someone's posture builds rapport. An open posture (uncrossed arms and legs) communicates receptiveness.\n\n**Facial expressions:** Your face can express emotions you are not even aware of. A furrowed brow might signal concern even when your words say \"I'm fine.\" Genuine smiles (which involve the eyes) versus polite smiles (mouth only) are universally recognized.\n\n**Tone of voice:** The same words can convey completely different messages depending on tone. \"That's fine\" can mean genuine acceptance or barely concealed frustration. Notice the volume, pace, pitch, and warmth of your voice.\n\n**Eye contact:** Appropriate eye contact signals confidence and engagement. Too little may suggest discomfort or disinterest. Too much can feel aggressive. Cultural norms around eye contact vary widely.\n\n**Physical space:** How close you stand to someone communicates intimacy, comfort, or boundary-setting. Respect others' personal space and notice when they adjust their distance.\n\nBecoming aware of your own nonverbal signals -- and learning to read others' -- dramatically improves your communication effectiveness and emotional intelligence.",
                    ExercisePrompt = "In your next interaction (in person or on a video call), pay close attention to 3 nonverbal cues: the other person's posture, their tone of voice, and their facial expressions. Did any of these nonverbal signals add information beyond their words?",
                    Order = 2
                },
                new LearningModule
                {
                    PathId = path3.Id,
                    Title = "Assertiveness",
                    Content = "Assertiveness is the ability to express your thoughts, feelings, and needs directly and respectfully. It sits in the healthy middle ground between passivity (suppressing your needs to avoid conflict) and aggression (imposing your needs at others' expense).\n\nSigns you might struggle with assertiveness:\n- You say \"yes\" when you want to say \"no\"\n- You feel guilty for having needs or opinions\n- You avoid expressing disagreement\n- You apologize excessively\n- You feel resentful because your needs go unmet\n\nBuilding assertiveness:\n\n**Know your rights:** You have the right to say no, to have your own opinions, to make mistakes, to change your mind, and to ask for what you need.\n\n**Use clear, direct language:** \"I'd prefer not to\" is more effective than \"I guess I could, but maybe...\" Say what you mean without excessive hedging.\n\n**The broken record technique:** If someone pushes back, calmly repeat your position: \"I understand, and I'm not available this weekend.\" You don't need to justify or argue.\n\n**Start small:** Practice assertiveness in low-stakes situations (returning an incorrect order, declining an invitation) before tackling more challenging conversations.\n\n**Remember:** Assertiveness is not about being aggressive or unkind. It is about honoring your own needs while respecting others'. Healthy relationships require both people to communicate honestly about their needs and boundaries.",
                    ExercisePrompt = "Practice saying \"no\" to one request this week -- something you would normally agree to despite not wanting to. Use a simple, respectful response like \"Thank you for thinking of me, but I'm not able to this time.\" How did it feel?",
                    Order = 3
                },
                new LearningModule
                {
                    PathId = path3.Id,
                    Title = "Conflict Resolution",
                    Content = "Conflict is a natural part of human relationships. The goal is not to eliminate conflict but to handle it constructively. Research by John Gottman shows that how couples handle conflict -- not whether they have it -- predicts relationship success.\n\nPrinciples of healthy conflict resolution:\n\n**Choose the right time and place:** Don't start difficult conversations when either person is tired, hungry, stressed, or in public. Schedule a time to talk if needed.\n\n**Define the issue clearly:** \"I want to talk about how we divide household responsibilities\" is better than \"We need to talk about everything you do wrong.\"\n\n**Listen first, speak second:** Seek to understand the other person's perspective before presenting your own. Use active listening skills.\n\n**Focus on the problem, not the person:** \"This situation is frustrating\" rather than \"You are so frustrating.\" Avoid character attacks, contempt, and generalizations (\"you always,\" \"you never\").\n\n**Look for common ground:** Most conflicts have shared interests underneath. Both people usually want to feel respected, valued, and heard. Start from what you agree on.\n\n**Brainstorm solutions together:** Move from \"I want\" vs. \"You want\" to \"What can we figure out together?\" Be willing to compromise.\n\n**Take breaks when needed:** If emotions escalate, agree to pause and return to the conversation when both people are calmer. Use a signal phrase like \"I need a 20-minute break.\"\n\n**Repair and reconnect:** After resolving a conflict, acknowledge the effort both of you made. Express appreciation for the other person's willingness to work through it.",
                    ExercisePrompt = "Reflect on a recent conflict (big or small). How was it handled? Were any of the principles above used or missed? If you could replay the conversation, what would you do differently?",
                    Order = 4
                }
            };

            // Path 4: Stress Management Toolkit
            var path4 = new LearningPath
            {
                Title = "Stress Management Toolkit",
                Description = "Equip yourself with practical tools to manage stress. From breathing techniques to time management, build a personal toolkit for staying calm under pressure.",
                Category = LearningCategory.StressManagement,
                EstimatedMinutes = 30,
                ModuleCount = 4
            };

            var path4Modules = new List<LearningModule>
            {
                new LearningModule
                {
                    PathId = path4.Id,
                    Title = "Understanding Stress",
                    Content = "Stress is your body's response to any demand or challenge. It triggers the \"fight-or-flight\" response: your heart beats faster, muscles tense, breathing quickens, and stress hormones like cortisol and adrenaline flood your system.\n\nShort-term stress can be helpful -- it sharpens focus, boosts energy, and motivates action. This is called \"eustress\" or positive stress. However, chronic stress -- when the stress response stays activated for extended periods -- can damage your physical and mental health.\n\nEffects of chronic stress include:\n- Weakened immune system\n- Digestive problems\n- Sleep disruption\n- Anxiety and depression\n- Difficulty concentrating\n- Irritability and mood swings\n- Cardiovascular issues\n\nUnderstanding your stress is the first step to managing it. Stress has three components:\n\n1. **The stressor:** The external event or situation (deadlines, conflicts, financial pressure)\n2. **Your perception:** How you interpret the stressor (\"This is a disaster\" vs. \"This is a challenge I can handle\")\n3. **The stress response:** Your body's physical and emotional reaction\n\nYou can manage stress by changing any of these three components: reducing stressors where possible, reframing your perception, or calming your body's stress response through techniques you'll learn in this path.",
                    ExercisePrompt = "Rate your current stress level on a scale of 1-10. Then identify the top 3 sources of stress in your life right now. For each one, note which component you have the most control over: the stressor itself, your perception of it, or your physical response.",
                    Order = 0
                },
                new LearningModule
                {
                    PathId = path4.Id,
                    Title = "Breathing Techniques",
                    Content = "Your breath is the most accessible stress-management tool you have. It is always with you and directly influences your nervous system. Slow, deep breathing activates the parasympathetic nervous system (your \"rest and digest\" mode), counteracting the stress response.\n\nHere are three powerful breathing techniques:\n\n**Box Breathing (4-4-4-4):**\n1. Inhale through your nose for 4 counts\n2. Hold your breath for 4 counts\n3. Exhale through your mouth for 4 counts\n4. Hold empty for 4 counts\n5. Repeat 4 cycles\n\nUsed by Navy SEALs and first responders to stay calm under extreme pressure.\n\n**4-7-8 Breathing:**\n1. Inhale through your nose for 4 counts\n2. Hold your breath for 7 counts\n3. Exhale slowly through your mouth for 8 counts\n4. Repeat 3-4 cycles\n\nDeveloped by Dr. Andrew Weil, this technique is particularly effective for falling asleep or calming anxiety.\n\n**Physiological Sigh (double inhale):**\n1. Take a quick inhale through your nose\n2. Immediately take a second, shorter inhale on top of it (to fully inflate the lungs)\n3. Exhale slowly through your mouth\n4. Repeat 2-3 times\n\nResearched by Stanford neuroscientist Andrew Huberman, this is the fastest known method to reduce stress in real-time -- it can work in a single breath cycle.\n\nThe key with breathing techniques is to make the exhale longer than the inhale. This signals safety to your nervous system.",
                    ExercisePrompt = "Try box breathing right now: inhale for 4 counts, hold for 4, exhale for 4, hold for 4. Complete 4 full cycles. How do you feel before versus after? Rate your stress level 1-10 before and after.",
                    Order = 1
                },
                new LearningModule
                {
                    PathId = path4.Id,
                    Title = "Progressive Muscle Relaxation",
                    Content = "Progressive Muscle Relaxation (PMR) is a technique developed by Dr. Edmund Jacobson in the 1930s. It is based on the principle that physical relaxation leads to mental relaxation. By systematically tensing and then releasing different muscle groups, you teach your body the difference between tension and relaxation.\n\nHow to practice PMR:\n\n1. Find a quiet, comfortable place. Sit or lie down.\n2. Close your eyes and take several slow, deep breaths.\n3. Start with your feet: tense the muscles as tightly as you can for 5 seconds.\n4. Release suddenly and notice the feeling of relaxation for 10-15 seconds.\n5. Move upward through each muscle group:\n   - Calves\n   - Thighs\n   - Glutes\n   - Abdomen\n   - Chest\n   - Hands (make fists)\n   - Forearms\n   - Upper arms (bicep curl)\n   - Shoulders (shrug toward ears)\n   - Neck\n   - Face (scrunch everything tight)\n6. After completing all groups, take a moment to scan your body and enjoy the overall sense of relaxation.\n\nPMR is particularly effective for:\n- Releasing physical tension you may not realize you're holding\n- Reducing headaches and chronic pain\n- Improving sleep quality\n- Lowering blood pressure\n- Managing anxiety before stressful events\n\nWith practice, you'll become more aware of when your body starts tensing up during the day and can release that tension before it builds.",
                    ExercisePrompt = "Do a 5-minute PMR session. Tense and release at least 5 major muscle groups (feet, legs, abdomen, hands, shoulders). Rate your physical tension on a 1-10 scale before and after. Which muscle group held the most tension?",
                    Order = 2
                },
                new LearningModule
                {
                    PathId = path4.Id,
                    Title = "Time Management",
                    Content = "Much of our daily stress comes not from the difficulty of individual tasks but from the feeling of having too much to do and too little time. Effective time management is therefore a powerful stress-reduction tool.\n\nKey principles:\n\n**The Eisenhower Matrix:** Categorize tasks into four quadrants:\n- **Urgent + Important:** Do these first (deadlines, emergencies)\n- **Important + Not Urgent:** Schedule these (exercise, planning, relationships) -- this is where most meaningful work lives\n- **Urgent + Not Important:** Delegate or minimize (most emails, some meetings)\n- **Not Urgent + Not Important:** Eliminate (mindless scrolling, unnecessary tasks)\n\n**The 3-3-3 Method:** Each day, plan to accomplish:\n- 3 hours of deep work on your most important project\n- 3 shorter tasks (emails, calls, errands)\n- 3 maintenance activities (exercise, tidying, meal prep)\n\n**Time blocking:** Assign specific blocks of time to specific types of work. Protect your deep-work blocks from interruptions.\n\n**The two-minute rule:** If a task takes less than two minutes, do it now rather than adding it to your list.\n\n**Learn to say no:** Every \"yes\" is a \"no\" to something else. Be intentional about what earns your time.\n\n**Build in buffer time:** Don't schedule back-to-back. Leave gaps for transitions, unexpected tasks, and mental rest.\n\nRemember: the goal is not to fill every minute with productivity. The goal is to spend your time on what matters most so you feel less overwhelmed and more in control.",
                    ExercisePrompt = "Write down your top 3 priorities for tomorrow. For each one, estimate how long it will take and schedule a specific time block for it. How does having a plan affect your stress level about tomorrow?",
                    Order = 3
                }
            };

            // Path 5: Building Resilience
            var path5 = new LearningPath
            {
                Title = "Building Resilience",
                Description = "Develop the mental strength to bounce back from setbacks. Learn about growth mindset, support networks, and self-compassion as pillars of resilience.",
                Category = LearningCategory.Resilience,
                EstimatedMinutes = 35,
                ModuleCount = 4
            };

            var path5Modules = new List<LearningModule>
            {
                new LearningModule
                {
                    PathId = path5.Id,
                    Title = "What Is Resilience?",
                    Content = "Resilience is not about being tough or never struggling. It is the ability to adapt and recover when life doesn't go as planned. Think of resilience not as a fixed trait you either have or don't, but as a set of skills and practices you can develop over time.\n\nResilience doesn't mean you won't experience difficulty, pain, or setbacks. It means you have the internal resources to process those experiences, learn from them, and eventually move forward.\n\nResearch identifies several key components of resilience:\n\n**Connection:** Having supportive relationships that provide encouragement and practical help.\n\n**Flexible thinking:** The ability to see setbacks as temporary and specific rather than permanent and all-encompassing.\n\n**Self-efficacy:** Belief in your ability to influence outcomes and solve problems.\n\n**Emotion regulation:** The capacity to manage strong emotions without being overwhelmed.\n\n**Meaning-making:** The ability to find purpose or growth in difficult experiences.\n\n**Self-care:** Maintaining the physical and emotional resources that fuel recovery.\n\nImportantly, resilience is not a solo endeavor. The myth of the \"rugged individual\" who overcomes everything alone is just that -- a myth. Resilient people lean on others, ask for help, and build support systems.\n\nThe modules ahead will help you strengthen each of these resilience components.",
                    ExercisePrompt = "Write about a challenge or setback you overcame in the past. What helped you get through it? Which resilience components (connection, flexible thinking, self-efficacy, emotion regulation, meaning-making, self-care) did you draw on?",
                    Order = 0
                },
                new LearningModule
                {
                    PathId = path5.Id,
                    Title = "Growth Mindset",
                    Content = "Psychologist Carol Dweck's research on mindset has transformed how we think about learning, failure, and potential. She identified two core mindsets:\n\n**Fixed mindset:** \"My abilities are innate and unchangeable. Failure means I'm not good enough.\"\n**Growth mindset:** \"My abilities can be developed through effort and learning. Failure is information about what to try next.\"\n\nPeople with a fixed mindset tend to avoid challenges (to protect their self-image), give up easily when things get hard, see effort as pointless (\"If I were talented, it wouldn't be this hard\"), ignore useful criticism, and feel threatened by others' success.\n\nPeople with a growth mindset embrace challenges as opportunities, persist through obstacles, see effort as the path to mastery, learn from criticism, and find inspiration in others' success.\n\nThe good news: mindset is not fixed. You can shift from fixed to growth through awareness and practice:\n\n1. **Notice your self-talk:** When you catch yourself thinking \"I can't do this,\" add \"yet\" -- \"I can't do this yet.\"\n2. **Reframe failure:** Instead of \"I failed,\" try \"I learned something important.\"\n3. **Praise process over outcome:** Focus on effort, strategy, and improvement rather than just results.\n4. **Embrace challenges:** When something is hard, recognize that difficulty is where growth happens.\n\nA growth mindset doesn't mean you'll succeed at everything. It means you'll learn from everything.",
                    ExercisePrompt = "Think of one recent setback or failure. Write it down, then reframe it as a learning opportunity. What did the experience teach you? What would you do differently next time? How has this setback potentially made you stronger?",
                    Order = 1
                },
                new LearningModule
                {
                    PathId = path5.Id,
                    Title = "Support Networks",
                    Content = "Humans are social creatures, and our connections with others are one of the strongest predictors of resilience, health, and well-being. Research consistently shows that people with strong social support recover faster from illness, handle stress better, and live longer.\n\nYour support network might include:\n\n**Inner circle:** The 2-5 people you can call at 3 AM. These are the people who know you deeply and love you unconditionally.\n\n**Close friends and family:** People you see regularly, share activities with, and can confide in about most things.\n\n**Community connections:** Colleagues, neighbors, group members, or acquaintances who provide belonging and practical support.\n\n**Professional support:** Therapists, coaches, mentors, or spiritual advisors who provide specialized guidance.\n\nBuilding and maintaining support networks:\n\n**Invest before you need:** Don't wait until crisis to build connections. Regular small investments in relationships create a safety net.\n\n**Be vulnerable:** Deep connections require honesty about struggles, not just sharing highlight reels.\n\n**Reciprocate:** Support is a two-way street. Be available for others as you'd want them available for you.\n\n**Diversify:** Different people serve different needs. A work mentor, a fun friend, a deep listener, a practical helper -- each role is valuable.\n\n**Maintain with small gestures:** A brief text, a shared article, a genuine \"How are you?\" -- connection doesn't require grand gestures.\n\nIf your support network feels thin, start small: join a group aligned with your interests, reconnect with an old friend, or reach out to a colleague for coffee.",
                    ExercisePrompt = "Identify 3 people you could reach out to for support. For each one, note what kind of support they offer (emotional, practical, fun, wisdom). Is there someone you've lost touch with that you'd like to reconnect with? What's one small step you could take this week?",
                    Order = 2
                },
                new LearningModule
                {
                    PathId = path5.Id,
                    Title = "Self-Compassion",
                    Content = "Self-compassion, as defined by researcher Kristin Neff, has three components:\n\n1. **Self-kindness:** Treating yourself with the same warmth and understanding you'd offer a good friend, rather than harsh self-criticism.\n\n2. **Common humanity:** Recognizing that suffering and imperfection are part of the shared human experience, rather than feeling isolated in your struggles.\n\n3. **Mindfulness:** Holding your pain in balanced awareness rather than ignoring it or over-identifying with it.\n\nMany people resist self-compassion because they believe self-criticism motivates them. Research shows the opposite: self-compassion actually increases motivation, resilience, and emotional well-being. People who practice self-compassion are more likely to try again after failure, take responsibility for mistakes (without spiraling into shame), and maintain healthy habits.\n\nPracticing self-compassion:\n\n**The self-compassion break:** When you notice you're struggling, pause and say:\n- \"This is a moment of suffering\" (mindfulness)\n- \"Suffering is part of life\" (common humanity)\n- \"May I be kind to myself\" (self-kindness)\n\n**Write a compassionate letter:** When you're being hard on yourself, write a letter from the perspective of an unconditionally loving friend. What would they say?\n\n**Change your self-talk:** Notice when your inner critic speaks. Would you talk to a friend this way? If not, revise the message.\n\n**Touch:** Place your hand on your heart or give yourself a hug. Physical gestures of comfort activate the care system in your brain.\n\nSelf-compassion is not self-pity, self-indulgence, or lowering your standards. It is giving yourself the same grace you'd give someone you love.",
                    ExercisePrompt = "Write yourself a brief compassionate letter about something you've been struggling with or criticizing yourself for. Address yourself as you would a dear friend -- with warmth, understanding, and encouragement. How does it feel to receive this message from yourself?",
                    Order = 3
                }
            };

            // Add all paths and modules
            _context.LearningPaths.AddRange(path1, path2, path3, path4, path5);
            _context.LearningModules.AddRange(path1Modules);
            _context.LearningModules.AddRange(path2Modules);
            _context.LearningModules.AddRange(path3Modules);
            _context.LearningModules.AddRange(path4Modules);
            _context.LearningModules.AddRange(path5Modules);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {PathCount} learning paths with modules", 5);
        }
    }
}
