import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { contactsApi } from '@/lib/api/contacts';
import {
  ArrowRight,
  Download,
  FileText,
  Github,
  GitPullRequest,
  MessageSquare,
  Play,
  Zap,
} from 'lucide-react';
import { useState } from 'react';

const Index = () => {
  const [submitting, setSubmitting] = useState(false);
  const [submitMessage, setSubmitMessage] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState(false);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [submitSuccess, setSubmitSuccess] = useState(false);

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <header className="border-b border-border bg-card/50 backdrop-blur-sm sticky top-0 z-50">
        <div className="container mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-lg bg-primary flex items-center justify-center">
              <MessageSquare className="w-5 h-5 text-primary-foreground" />
            </div>
            <span className="font-semibold text-xl">Chapi</span>
          </div>
          <nav className="hidden md:flex items-center gap-6">
            <a
              href="#how-it-works"
              className="text-muted-foreground hover:text-accent transition-colors"
            >
              How it works
            </a>
            <a
              href="#"
              className="text-muted-foreground hover:text-accent transition-colors"
            >
              Docs
            </a>
            <Button asChild variant="outline" size="sm">
              <a
                href="https://github.com/chapi-dev/chapi"
                target="_blank"
                rel="noopener noreferrer"
              >
                <Github className="w-4 h-4 mr-2" />
                GitHub
              </a>
            </Button>
          </nav>
        </div>
      </header>

      {toastMessage && (
        <div className="fixed top-6 right-6 bg-card border px-4 py-2 rounded shadow">
          <div className="text-sm">{toastMessage}</div>
        </div>
      )}

      {/* Hero Section */}
      <section className="py-20 px-6">
        <div className="container mx-auto text-center max-w-4xl">
          <Badge
            variant="outline"
            className="mb-6 bg-accent/10 text-accent border-accent/20"
          >
            <Zap className="w-3 h-3 mr-1" />
            Chat-first API testing
          </Badge>

          <h1 className="text-5xl md:text-6xl font-bold mb-6 leading-tight">
            Chat your API tests.{' '}
            <span className="text-transparent bg-gradient-to-r from-primary to-accent bg-clip-text">
              Chapi runs them.
            </span>
          </h1>

          <p className="text-xl text-muted-foreground mb-8 max-w-2xl mx-auto leading-relaxed">
            Generate comprehensive API tests through natural conversation. Run
            locally, in cloud, or integrate with your PR workflow. Zero
            configuration, maximum coverage.
          </p>

          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            {/* Join Beta opens a dialog with a small signup form */}
            <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
              <DialogTrigger asChild>
                <Button
                  size="lg"
                  className="text-lg h-12 px-8"
                  onClick={() => setIsDialogOpen(true)}
                >
                  Join Beta
                  <ArrowRight className="w-5 h-5 ml-2" />
                </Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Join the Beta</DialogTitle>
                  <DialogDescription>
                    Tell us a little about yourself and we'll send an invite
                    when available.
                  </DialogDescription>
                </DialogHeader>

                <form
                  className="space-y-4 mt-4"
                  onSubmit={async e => {
                    e.preventDefault();
                    const form = e.currentTarget as HTMLFormElement;
                    const data = {
                      name: (
                        form.elements.namedItem('name') as HTMLInputElement
                      )?.value,
                      email: (
                        form.elements.namedItem('email') as HTMLInputElement
                      )?.value,
                      company: (
                        form.elements.namedItem('company') as HTMLInputElement
                      )?.value,
                    };

                    try {
                      setSubmitting(true);
                      await contactsApi.create(data);
                      setSubmitMessage(
                        'Thanks — we saved your info and will be in touch.'
                      );
                      setSubmitError(false);
                      // show success state inside dialog (hide inputs)
                      setSubmitSuccess(true);
                      // optionally reset form
                      form.reset();
                    } catch (err) {
                      console.error('Failed to save contact', err);
                      setSubmitMessage(
                        'Unable to save — please try again later.'
                      );
                      setSubmitError(true);
                    } finally {
                      setSubmitting(false);
                    }
                  }}
                >
                  {!submitSuccess ? (
                    <>
                      <div>
                        <Label htmlFor="name">Name</Label>
                        <Input
                          id="name"
                          name="name"
                          placeholder="Your full name"
                          required
                        />
                      </div>

                      <div>
                        <Label htmlFor="email">Email</Label>
                        <Input
                          id="email"
                          name="email"
                          type="email"
                          placeholder="you@example.com"
                          required
                        />
                      </div>

                      <div>
                        <Label htmlFor="company">Company (optional)</Label>
                        <Input
                          id="company"
                          name="company"
                          placeholder="Company name"
                        />
                      </div>
                    </>
                  ) : (
                    <div className="py-6 text-center">
                      <div className="text-lg font-semibold mb-2">
                        Thanks — we saved your info and will be in touch.
                      </div>
                      <div className="text-sm text-muted-foreground mb-4">
                        You can close this dialog when ready.
                      </div>
                    </div>
                  )}

                  <p className="text-sm text-muted-foreground">
                    We won't spam you — we'll only send beta invites and
                    important updates.
                  </p>

                  <DialogFooter>
                    {!submitSuccess ? (
                      <>
                        <Button type="submit" disabled={submitting}>
                          {submitting ? 'Saving...' : 'Join'}
                        </Button>
                        <DialogClose asChild>
                          <Button variant="outline">Cancel</Button>
                        </DialogClose>
                      </>
                    ) : (
                      <DialogClose asChild>
                        <Button>Close</Button>
                      </DialogClose>
                    )}
                  </DialogFooter>
                  {submitMessage && (
                    <div
                      className={`mt-2 text-sm ${
                        submitError ? 'text-destructive' : 'text-green-600'
                      }`}
                    >
                      {submitMessage}
                    </div>
                  )}
                </form>
              </DialogContent>
            </Dialog>
            <Button variant="outline" size="lg" className="text-lg h-12 px-8">
              <FileText className="w-5 h-5 mr-2" />
              Read Docs
            </Button>
          </div>
        </div>
      </section>

      {/* How it Works */}
      <section id="how-it-works" className="py-20 px-6 bg-muted/30">
        <div className="container mx-auto max-w-6xl">
          <div className="text-center mb-16">
            <h2 className="text-3xl font-bold mb-4">How it works</h2>
            <p className="text-muted-foreground text-lg max-w-2xl mx-auto">
              From conversation to comprehensive testing in minutes
            </p>
          </div>

          <div className="grid md:grid-cols-4 gap-8">
            <div className="text-center">
              <div className="w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center mx-auto mb-4">
                <MessageSquare className="w-6 h-6 text-primary" />
              </div>
              <h3 className="font-semibold mb-2">Describe in chat</h3>
              <p className="text-muted-foreground text-sm">
                Tell Chapi what you want to test using natural language or slash
                commands
              </p>
            </div>

            <div className="text-center">
              <div className="w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center mx-auto mb-4">
                <FileText className="w-6 h-6 text-primary" />
              </div>
              <h3 className="font-semibold mb-2">Generate tests</h3>
              <p className="text-muted-foreground text-sm">
                AI creates comprehensive test suites with edge cases and
                validations
              </p>
            </div>

            <div className="text-center">
              <div className="w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center mx-auto mb-4">
                <Play className="w-6 h-6 text-primary" />
              </div>
              <h3 className="font-semibold mb-2">Run locally/cloud</h3>
              <p className="text-muted-foreground text-sm">
                Execute tests in your environment or our cloud infrastructure
              </p>
            </div>

            <div className="text-center">
              <div className="w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center mx-auto mb-4">
                <GitPullRequest className="w-6 h-6 text-primary" />
              </div>
              <h3 className="font-semibold mb-2">PR checks</h3>
              <p className="text-muted-foreground text-sm">
                Integrate with GitHub for automated testing on every pull
                request
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Run Pack Teaser */}
      <section className="py-20 px-6">
        <div className="container mx-auto max-w-4xl">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold mb-4">
              Download portable test packs
            </h2>
            <p className="text-muted-foreground text-lg">
              Get everything you need to run tests anywhere
            </p>
          </div>

          <Card className="bg-card border shadow-soft">
            <CardContent className="p-8">
              <div className="font-mono text-sm">
                <div className="flex items-center gap-2 mb-4">
                  <Download className="w-4 h-4 text-accent" />
                  <span className="text-accent font-medium">
                    run-pack-user-service.zip
                  </span>
                </div>
                <div className="space-y-1 text-muted-foreground">
                  <div>├── tests.json</div>
                  <div>├── run.sh</div>
                  <div>├── run.ps1</div>
                  <div>├── .env.example</div>
                  <div>└── README.md</div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-border py-12 px-6">
        <div className="container mx-auto max-w-6xl">
          <div className="grid md:grid-cols-4 gap-8">
            <div>
              <div className="flex items-center gap-2 mb-4">
                <div className="w-6 h-6 rounded bg-primary flex items-center justify-center">
                  <MessageSquare className="w-4 h-4 text-primary-foreground" />
                </div>
                <span className="font-semibold">Chapi</span>
              </div>
              <p className="text-muted-foreground text-sm">
                Chat-first API testing for modern development teams.
              </p>
            </div>

            <div>
              <h4 className="font-medium mb-3">Product</h4>
              <div className="space-y-2 text-sm text-muted-foreground">
                <div>
                  <a href="#" className="hover:text-accent transition-colors">
                    Documentation
                  </a>
                </div>
                <div>
                  <a href="#" className="hover:text-accent transition-colors">
                    API Reference
                  </a>
                </div>
                <div>
                  <a href="#" className="hover:text-accent transition-colors">
                    Examples
                  </a>
                </div>
              </div>
            </div>

            <div>
              <h4 className="font-medium mb-3">Company</h4>
              <div className="space-y-2 text-sm text-muted-foreground">
                <div>
                  <a href="#" className="hover:text-accent transition-colors">
                    About
                  </a>
                </div>
                <div>
                  <a href="#" className="hover:text-accent transition-colors">
                    Privacy
                  </a>
                </div>
                <div>
                  <a href="#" className="hover:text-accent transition-colors">
                    Terms
                  </a>
                </div>
              </div>
            </div>

            <div>
              <h4 className="font-medium mb-3">Status</h4>
              <div className="space-y-2 text-sm">
                <div className="flex items-center gap-2">
                  <div className="w-2 h-2 rounded-full bg-green-500"></div>
                  <span className="text-muted-foreground">
                    All systems operational
                  </span>
                </div>
                <div>
                  <a href="#" className="text-accent hover:underline">
                    Status page
                  </a>
                </div>
              </div>
            </div>
          </div>

          <div className="border-t border-border mt-8 pt-8 text-center text-sm text-muted-foreground">
            © 2024 Chapi. All rights reserved.
          </div>
        </div>
      </footer>
    </div>
  );
};

export default Index;
