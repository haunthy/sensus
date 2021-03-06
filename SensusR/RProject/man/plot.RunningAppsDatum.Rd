% Generated by roxygen2: do not edit by hand
% Please edit documentation in R/SensusR.R
\name{plot.RunningAppsDatum}
\alias{plot.RunningAppsDatum}
\title{Plot running apps data.}
\usage{
\method{plot}{RunningAppsDatum}(x, ...)
}
\arguments{
\item{x}{Apps data.}

\item{...}{Other plotting parameters.}
}
\description{
Plot running apps data.
}
\examples{
data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
plot(data$RunningAppsDatum)
}

