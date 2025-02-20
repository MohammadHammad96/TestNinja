﻿using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TestNinja.Mocking;

// ReSharper disable InconsistentNaming

namespace TestNinja.UnitTests.Mocking
{
    [TestFixture]
    internal class HouseKepper_SendStatementEmailsTests
    {
        private Mock<IUnitOfWork> _unitOfWork;
        private Mock<IStatementGenerator> _statementGenerator;
        private Mock<IEmailSender> _emailSender;
        private HouseKepperService _service;
        private Mock<IXtraMessageBox> _messageBox;
        private readonly DateTime _statementDate = new(2022, 4, 29);
        private Housekeeper _housekeeper;
        private string _statementFileName;

        [SetUp]
        public void SetUp()
        {
            _housekeeper = new Housekeeper
            {
                Email = "a",
                FullName = "b",
                Oid = 1,
                StatementEmailBody = "c"
            };

            _unitOfWork = new Mock<IUnitOfWork>();
            _unitOfWork.Setup(uow => uow.Query<Housekeeper>()).Returns(new List<Housekeeper>
            {
                _housekeeper
            }.AsQueryable());

            _statementFileName = "filename";
            _statementGenerator = new Mock<IStatementGenerator>();
            _statementGenerator
                .Setup(sg => sg.SaveStatement(_housekeeper.Oid, _housekeeper.FullName, _statementDate))
                .Returns(() => _statementFileName);

            _emailSender = new Mock<IEmailSender>();
            _messageBox = new Mock<IXtraMessageBox>();

            _service = new HouseKepperService(
                _unitOfWork.Object,
                _statementGenerator.Object,
                _emailSender.Object,
                _messageBox.Object);
        }

        [Test]
        public void WhenCalled_GenerateStatements()
        {
            _service.SendStatementEmails(_statementDate);

            _statementGenerator.Verify(sg =>
                sg.SaveStatement(_housekeeper.Oid, _housekeeper.FullName, _statementDate));
        }

        [Test]
        [TestCase(null)]
        [TestCase(" ")]
        [TestCase("")]
        public void HouseKepperEmailIsNullOrWhiteSpaceOrEmpty_ShouldNotGenerateStatement(string email)
        {
            _housekeeper.Email = email;

            _service.SendStatementEmails(_statementDate);

            _statementGenerator.Verify(sg =>
                sg.SaveStatement(_housekeeper.Oid, _housekeeper.FullName, _statementDate),
                Times.Never);
        }

        [Test]
        public void WhenCalled_EmailTheStatement()
        {
            _service.SendStatementEmails(_statementDate);

            VerifyEmailSent();
        }

        [Test]
        [TestCase(null)]
        [TestCase(" ")]
        [TestCase("")]
        public void StatementFileNameIsNullOrWhiteSpaceOrEmpty_ShouldNotEmailTheStatement(string fileName)
        {
            _statementFileName = fileName;

            _service.SendStatementEmails(_statementDate);

            VerifyEmailNotSent();
        }

        [Test]
        public void EmailSendingFails_DisplayAMessageBox()
        {
            _emailSender.Setup(es => es.EmailFile(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
                )).Throws<Exception>();

            _service.SendStatementEmails(_statementDate);

            _messageBox.Verify(mb => mb.Show(
                It.IsAny<string>(),
                It.IsAny<string>(),
                MessageBoxButtons.Ok
                ));
        }

        private void VerifyEmailSent()
        {
            _emailSender.Verify(es => es.EmailFile(
                _housekeeper.Email,
                _housekeeper.StatementEmailBody,
                _statementFileName,
                It.IsAny<string>()));
        }

        private void VerifyEmailNotSent()
        {
            _emailSender.Verify(es => es.EmailFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }
    }
}